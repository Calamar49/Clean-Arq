using Hotel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pagos.Application.Configuracion;
using Pagos.Application.DTOs;
using Pagos.Application.Interfaces;
using Pagos.Domain.Entities;

namespace Pagos.Application.Services
{
    // Atiende el cobro de una reserva dentro de la Saga.
    public class PagoService : IPagoService
    {
        private readonly IPagoRepository _repository;
        private readonly IPasarelaPago _pasarela;
        private readonly IEventPublisher _eventos;
        private readonly ILogger<PagoService> _log;
        private readonly string _moneda;

        public PagoService(
            IPagoRepository repository,
            IPasarelaPago pasarela,
            IEventPublisher eventos,
            ILogger<PagoService> log,
            IOptions<PasarelaOptions> opciones)
        {
            _repository = repository;
            _pasarela = pasarela;
            _eventos = eventos;
            _log = log;
            _moneda = opciones.Value.Moneda;
        }

        public async Task ProcesarCobroAsync(
            Guid messageId,
            Guid reservaId,
            decimal monto,
            string huespedEmail,
            string metodoPago,
            CancellationToken ct)
        {
            // Idempotencia: si el mensaje ya se atendió, no se vuelve a cobrar.
            if (await _repository.YaProcesadoAsync(messageId))
            {
                _log.LogInformation(
                    "Mensaje {MessageId} ya procesado: no se repite el cobro.", messageId);
                return;
            }

            // Segunda salvaguarda: la reserva podría tener ya un pago registrado
            // aunque llegue un mensaje distinto para la misma reserva.
            var existente = await _repository.ObtenerPorReservaAsync(reservaId);
            if (existente is not null)
            {
                _log.LogInformation(
                    "La reserva {ReservaId} ya tiene un pago registrado ({Estado}).",
                    reservaId, existente.Estado);

                await _repository.RegistrarProcesadoAsync(messageId, RoutingKeys.ReservaBloqueada);
                return;
            }

            var resultado = await _pasarela.CobrarAsync(
                monto, _moneda, $"Reserva de hotel {reservaId}", metodoPago, ct);

            var pago = resultado.Aprobado
                ? Pago.Aprobado(reservaId, monto, _moneda, _pasarela.Nombre, resultado.Referencia!)
                : Pago.Rechazado(reservaId, monto, _moneda, _pasarela.Nombre, resultado.Motivo!);

            await _repository.AgregarAsync(pago);
            await _repository.RegistrarProcesadoAsync(messageId, RoutingKeys.ReservaBloqueada);

            if (resultado.Aprobado)
            {
                _log.LogInformation(
                    "Pago aprobado para la reserva {ReservaId} ({Referencia}).",
                    reservaId, resultado.Referencia);

                await _eventos.PublicarAsync(
                    new PagoAprobado(reservaId, resultado.Referencia!, monto), ct);
            }
            else
            {
                // El rechazo dispara la compensación: Reservas rechazará la
                // reserva y Habitaciones liberará las noches bloqueadas.
                _log.LogWarning(
                    "Pago rechazado para la reserva {ReservaId}: {Motivo}",
                    reservaId, resultado.Motivo);

                await _eventos.PublicarAsync(
                    new PagoRechazado(reservaId, resultado.Motivo!), ct);
            }
        }

        public async Task<List<PagoDTO>> ListarAsync(Guid? reservaId)
        {
            var pagos = await _repository.ListarAsync(reservaId);

            return pagos.Select(p => new PagoDTO
            {
                Id = p.Id,
                ReservaId = p.ReservaId,
                Monto = p.Monto,
                Moneda = p.Moneda,
                Estado = p.Estado.ToString(),
                Pasarela = p.Pasarela,
                Referencia = p.Referencia,
                Motivo = p.Motivo,
                FechaCreacion = p.FechaCreacion
            }).ToList();
        }
    }
}
