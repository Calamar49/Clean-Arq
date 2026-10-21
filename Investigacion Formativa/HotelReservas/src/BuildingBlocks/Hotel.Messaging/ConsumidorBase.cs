using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hotel.Messaging
{
    // Consumidor base de la Saga. Cada servicio hereda de esta clase e implementa
    // únicamente la lógica de negocio de sus eventos.
    //
    // Esta clase se encarga de:
    //   - declarar el exchange y las colas del servicio
    //   - procesar cada mensaje dentro de su propio ámbito de dependencias
    //   - reintentar los fallos transitorios mediante la cola de reintento
    //   - enviar a la cola de errores lo que falla tras agotar los intentos
    public abstract class ConsumidorBase : BackgroundService
    {
        private const string CabeceraIntentos = "x-intentos";
        private const string CabeceraRoutingKey = "x-routing-key-original";

        private readonly RabbitMqConexion _conexion;
        private readonly IServiceScopeFactory _ambitos;
        private readonly RabbitMqOptions _opciones;
        private readonly ILogger _log;

        protected ConsumidorBase(
            RabbitMqConexion conexion,
            IServiceScopeFactory ambitos,
            IOptions<RabbitMqOptions> opciones,
            ILogger log)
        {
            _conexion = conexion;
            _ambitos = ambitos;
            _opciones = opciones.Value;
            _log = log;
        }

        // Nombre corto del servicio: determina los nombres de sus colas.
        protected abstract string Servicio { get; }

        // Claves de enrutamiento a las que se suscribe este servicio.
        protected abstract string[] RoutingKeys { get; }

        // Lógica de negocio del evento recibido.
        protected abstract Task ManejarAsync(
            string routingKey, string json, IServiceProvider servicios, CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Si RabbitMQ aún no está disponible al arrancar, se reintenta.
            IChannel canal = await ConectarConReintentosAsync(ct);

            var consumidor = new AsyncEventingBasicConsumer(canal);
            consumidor.ReceivedAsync += (_, ea) => ProcesarAsync(canal, ea, ct);

            await canal.BasicConsumeAsync(
                queue: Topologia.ColaPrincipal(Servicio),
                autoAck: false,
                consumer: consumidor,
                cancellationToken: ct);

            _log.LogInformation(
                "Consumidor de {Servicio} escuchando en {Cola}",
                Servicio, Topologia.ColaPrincipal(Servicio));

            // Mantiene vivo el servicio en segundo plano.
            await Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => { }, CancellationToken.None);
        }

        private async Task<IChannel> ConectarConReintentosAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var conexion = await _conexion.ObtenerAsync(ct);
                    var canal = await conexion.CreateChannelAsync(cancellationToken: ct);

                    await Topologia.DeclararAsync(canal, _opciones, Servicio, RoutingKeys, ct);

                    // Limita cuántos mensajes recibe el consumidor sin confirmar.
                    await canal.BasicQosAsync(0, _opciones.Prefetch, false, ct);

                    return canal;
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex,
                        "No se pudo conectar a RabbitMQ. Reintentando en 5 segundos...");
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }

            throw new OperationCanceledException(ct);
        }

        private async Task ProcesarAsync(IChannel canal, BasicDeliverEventArgs ea, CancellationToken ct)
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            // Al volver desde la cola de reintento, el mensaje llega por el exchange
            // por defecto y pierde su clave original; por eso se conserva en una cabecera.
            var routingKey = LeerCabecera(ea, CabeceraRoutingKey) ?? ea.RoutingKey;
            var intentos = LeerIntentos(ea);

            try
            {
                // Cada mensaje se procesa en su propio ámbito para poder usar
                // servicios con ciclo de vida por petición, como el DbContext.
                using var ambito = _ambitos.CreateScope();
                await ManejarAsync(routingKey, json, ambito.ServiceProvider, ct);

                await canal.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (EventoIgnorableException ex)
            {
                // Reintentarlo no cambiaría el resultado: se descarta.
                _log.LogInformation("Evento {RoutingKey} ignorado: {Motivo}", routingKey, ex.Message);
                await canal.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                await ManejarFalloAsync(canal, ea, routingKey, json, intentos, ex, ct);
            }
        }

        private async Task ManejarFalloAsync(
            IChannel canal,
            BasicDeliverEventArgs ea,
            string routingKey,
            string json,
            int intentos,
            Exception ex,
            CancellationToken ct)
        {
            var siguienteIntento = intentos + 1;
            var cuerpo = Encoding.UTF8.GetBytes(json);

            var propiedades = new BasicProperties
            {
                MessageId = ea.BasicProperties.MessageId,
                ContentType = "application/json",
                Persistent = true,
                Headers = new Dictionary<string, object?>
                {
                    [CabeceraIntentos] = siguienteIntento,
                    [CabeceraRoutingKey] = routingKey
                }
            };

            if (siguienteIntento < _opciones.MaxIntentos)
            {
                _log.LogWarning(ex,
                    "Fallo al procesar {RoutingKey}. Reintento {Intento}/{Max} en {Retraso} ms",
                    routingKey, siguienteIntento, _opciones.MaxIntentos, _opciones.RetrasoReintentoMs);

                // La cola de reintento retiene el mensaje y lo devuelve al vencer el TTL.
                await canal.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: Topologia.ColaReintento(Servicio),
                    mandatory: false,
                    basicProperties: propiedades,
                    body: cuerpo,
                    cancellationToken: ct);
            }
            else
            {
                _log.LogError(ex,
                    "Evento {RoutingKey} descartado tras {Intentos} intentos. Va a la cola de errores.",
                    routingKey, siguienteIntento);

                await canal.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: Topologia.ColaErrores(Servicio),
                    mandatory: false,
                    basicProperties: propiedades,
                    body: cuerpo,
                    cancellationToken: ct);
            }

            await canal.BasicAckAsync(ea.DeliveryTag, false, ct);
        }

        private static int LeerIntentos(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties.Headers is null ||
                !ea.BasicProperties.Headers.TryGetValue(CabeceraIntentos, out var valor))
            {
                return 0;
            }

            return valor switch
            {
                int numero => numero,
                long numero => (int)numero,
                byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var n) => n,
                _ => 0
            };
        }

        private static string? LeerCabecera(BasicDeliverEventArgs ea, string nombre)
        {
            if (ea.BasicProperties.Headers is null ||
                !ea.BasicProperties.Headers.TryGetValue(nombre, out var valor))
            {
                return null;
            }

            return valor switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string texto => texto,
                _ => null
            };
        }
    }
}
