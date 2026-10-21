using Hotel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pagos.Application.Configuracion;
using Pagos.Application.Interfaces;

namespace Pagos.Infrastructure.Pasarelas
{
    // Pasarela de pago simulada.
    //
    // Permite ejecutar y demostrar el sistema completo sin conexión a internet
    // y, sobre todo, provocar un rechazo cuando se quiera para evidenciar la
    // compensación de la Saga.
    public class PasarelaSimulada : IPasarelaPago
    {
        private readonly SimuladaOptions _opciones;
        private readonly ILogger<PasarelaSimulada> _log;

        public PasarelaSimulada(IOptions<PasarelaOptions> opciones, ILogger<PasarelaSimulada> log)
        {
            _opciones = opciones.Value.Simulada;
            _log = log;
        }

        public string Nombre => "Simulada";

        public async Task<ResultadoCobro> CobrarAsync(
            decimal monto,
            string moneda,
            string descripcion,
            string metodoPago,
            CancellationToken ct = default)
        {
            // Retraso artificial para que el comportamiento se parezca al de
            // una pasarela real y la Saga no se resuelva de forma instantánea.
            if (_opciones.RetrasoMs > 0)
                await Task.Delay(_opciones.RetrasoMs, ct);

            // La tarjeta elegida por el huésped manda sobre la configuración:
            // así la demostración resulta determinista desde la propia interfaz.
            if (metodoPago == MetodosPago.TarjetaRechazada)
            {
                _log.LogWarning(
                    "Cobro simulado rechazado por la tarjeta elegida: {Monto} {Moneda}",
                    monto, moneda.ToUpperInvariant());

                return ResultadoCobro.Fallo("La tarjeta fue rechazada por la entidad emisora.");
            }

            var aprobado = _opciones.Modo switch
            {
                ModoSimulado.AprobarSiempre => true,
                ModoSimulado.RechazarSiempre => false,
                _ => Random.Shared.NextDouble() < _opciones.TasaAprobacion
            };

            if (aprobado)
            {
                var referencia = $"SIM-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
                _log.LogInformation(
                    "Cobro simulado aprobado: {Monto} {Moneda} ({Referencia})",
                    monto, moneda.ToUpperInvariant(), referencia);

                return ResultadoCobro.Exito(referencia);
            }

            _log.LogWarning("Cobro simulado rechazado: {Monto} {Moneda}", monto, moneda.ToUpperInvariant());
            return ResultadoCobro.Fallo("La tarjeta fue rechazada por la entidad emisora.");
        }
    }
}
