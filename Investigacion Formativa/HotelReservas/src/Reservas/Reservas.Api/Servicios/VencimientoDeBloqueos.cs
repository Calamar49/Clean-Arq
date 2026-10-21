using Microsoft.Extensions.Options;
using Reservas.Application.Configuracion;
using Reservas.Application.Interfaces;

namespace Reservas.Api.Servicios
{
    // Revisa periódicamente si alguna reserva lleva demasiado tiempo bloqueada
    // y, en ese caso, la rechaza para que sus noches vuelvan al inventario.
    //
    // Sin esto, un fallo prolongado del servicio de Pagos dejaría habitaciones
    // retenidas indefinidamente: ningún evento llegaría a liberarlas.
    public class VencimientoDeBloqueos : BackgroundService
    {
        private readonly IServiceScopeFactory _ambitos;
        private readonly VencimientoOptions _opciones;
        private readonly ILogger<VencimientoDeBloqueos> _log;

        public VencimientoDeBloqueos(
            IServiceScopeFactory ambitos,
            IOptions<VencimientoOptions> opciones,
            ILogger<VencimientoDeBloqueos> log)
        {
            _ambitos = ambitos;
            _opciones = opciones.Value;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            if (!_opciones.Activo)
            {
                _log.LogWarning(
                    "El vencimiento de bloqueos está desactivado: las reservas que queden " +
                    "bloqueadas retendrán sus noches hasta que se resuelvan a mano.");
                return;
            }

            var limite = TimeSpan.FromMinutes(_opciones.MinutosLimite);
            var intervalo = TimeSpan.FromSeconds(_opciones.IntervaloSegundos);

            _log.LogInformation(
                "Vencimiento de bloqueos activo: revisión cada {Intervalo} y liberación de " +
                "las reservas que lleven más de {Limite} bloqueadas.", intervalo, limite);

            using var reloj = new PeriodicTimer(intervalo);

            try
            {
                // La primera revisión se hace al arrancar, para resolver lo que
                // hubiera quedado pendiente de una ejecución anterior.
                do
                {
                    await RevisarAsync(limite, ct);
                }
                while (await reloj.WaitForNextTickAsync(ct));
            }
            catch (OperationCanceledException)
            {
                // Apagado normal del servicio.
            }
        }

        private async Task RevisarAsync(TimeSpan limite, CancellationToken ct)
        {
            try
            {
                using var ambito = _ambitos.CreateScope();
                var reservas = ambito.ServiceProvider.GetRequiredService<IReservaService>();

                var liberadas = await reservas.VencerBloqueosAsync(limite, ct);

                if (liberadas > 0)
                {
                    _log.LogWarning(
                        "Se liberaron {Liberadas} reserva(s) por vencimiento del bloqueo.",
                        liberadas);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Un fallo puntual (la base de datos caída, por ejemplo) no debe
                // detener el servicio: se vuelve a intentar en la siguiente vuelta.
                _log.LogError(ex, "Falló la revisión de bloqueos vencidos.");
            }
        }
    }
}
