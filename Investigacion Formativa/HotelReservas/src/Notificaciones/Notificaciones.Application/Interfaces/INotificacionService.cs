using Notificaciones.Application.DTOs;
using Notificaciones.Domain.Entities;

namespace Notificaciones.Application.Interfaces
{
    public interface INotificacionRepository
    {
        Task<List<Notificacion>> ListarAsync(string? destinatario, Guid? reservaId);

        Task AgregarAsync(Notificacion notificacion);

        Task<bool> YaProcesadoAsync(Guid messageId);

        Task RegistrarProcesadoAsync(Guid messageId, string routingKey);
    }

    public interface INotificacionService
    {
        // Registra el aviso correspondiente al evento recibido.
        Task RegistrarAsync(
            Guid messageId,
            string routingKey,
            Guid reservaId,
            string huespedNombre,
            string huespedEmail,
            string? detalle,
            CancellationToken ct);

        Task<List<NotificacionDTO>> ListarAsync(string? destinatario, Guid? reservaId);
    }
}
