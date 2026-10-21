using System.Text;
using System.Text.Json;
using Hotel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Hotel.Messaging
{
    // Publica los eventos de la Saga en el exchange de tipo topic.
    public class RabbitMqPublisher : IEventPublisher
    {
        private readonly RabbitMqConexion _conexion;
        private readonly RabbitMqOptions _opciones;
        private readonly ILogger<RabbitMqPublisher> _log;

        public RabbitMqPublisher(
            RabbitMqConexion conexion,
            IOptions<RabbitMqOptions> opciones,
            ILogger<RabbitMqPublisher> log)
        {
            _conexion = conexion;
            _opciones = opciones.Value;
            _log = log;
        }

        public async Task PublicarAsync<T>(T evento, CancellationToken ct = default)
            where T : IEvento
        {
            var conexion = await _conexion.ObtenerAsync(ct);
            await using var canal = await conexion.CreateChannelAsync(cancellationToken: ct);

            await canal.ExchangeDeclareAsync(
                exchange: _opciones.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            var json = JsonSerializer.Serialize(evento, evento.GetType());
            var cuerpo = Encoding.UTF8.GetBytes(json);

            var propiedades = new BasicProperties
            {
                MessageId = evento.MessageId.ToString(),
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.Now.ToUnixTimeSeconds()),

                // Los mensajes sobreviven a un reinicio del broker.
                Persistent = true
            };

            await canal.BasicPublishAsync(
                exchange: _opciones.Exchange,
                routingKey: evento.RoutingKey,
                mandatory: false,
                basicProperties: propiedades,
                body: cuerpo,
                cancellationToken: ct);

            _log.LogInformation(
                "Evento publicado {RoutingKey} ({MessageId})",
                evento.RoutingKey, evento.MessageId);
        }
    }
}
