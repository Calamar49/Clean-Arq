using Pagos.Domain.Entities;

namespace Pagos.Application.Interfaces
{
    public interface IPagoRepository
    {
        Task<List<Pago>> ListarAsync(Guid? reservaId);

        Task<Pago?> ObtenerPorReservaAsync(Guid reservaId);

        Task AgregarAsync(Pago pago);

        // Comprueba si un mensaje ya fue procesado (idempotencia).
        Task<bool> YaProcesadoAsync(Guid messageId);

        Task RegistrarProcesadoAsync(Guid messageId, string routingKey);
    }
}
