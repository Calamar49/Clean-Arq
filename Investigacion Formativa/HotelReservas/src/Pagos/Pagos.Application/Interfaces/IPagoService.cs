using Pagos.Application.DTOs;

namespace Pagos.Application.Interfaces
{
    public interface IPagoService
    {
        // Procesa el cobro de una reserva y publica el resultado en la Saga.
        Task ProcesarCobroAsync(
            Guid messageId,
            Guid reservaId,
            decimal monto,
            string huespedEmail,
            string metodoPago,
            CancellationToken ct);

        Task<List<PagoDTO>> ListarAsync(Guid? reservaId);
    }
}
