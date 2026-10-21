using Microsoft.EntityFrameworkCore;
using Notificaciones.Application.Interfaces;
using Notificaciones.Domain.Entities;
using Notificaciones.Infrastructure.Persistence;

namespace Notificaciones.Infrastructure.Repositories
{
    public class NotificacionRepository : INotificacionRepository
    {
        private readonly NotificacionesDbContext _db;

        public NotificacionRepository(NotificacionesDbContext db)
        {
            _db = db;
        }

        public Task<List<Notificacion>> ListarAsync(string? destinatario, Guid? reservaId)
        {
            var consulta = _db.Notificaciones.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(destinatario))
            {
                var normalizado = destinatario.Trim().ToLowerInvariant();
                consulta = consulta.Where(n => n.Destinatario == normalizado);
            }

            if (reservaId.HasValue)
                consulta = consulta.Where(n => n.ReservaId == reservaId.Value);

            return consulta.OrderByDescending(n => n.FechaEnvio).ToListAsync();
        }

        public async Task AgregarAsync(Notificacion notificacion)
        {
            _db.Notificaciones.Add(notificacion);
            await _db.SaveChangesAsync();
        }

        public Task<bool> YaProcesadoAsync(Guid messageId)
        {
            return _db.MensajesProcesados.AnyAsync(m => m.MessageId == messageId);
        }

        public async Task RegistrarProcesadoAsync(Guid messageId, string routingKey)
        {
            _db.MensajesProcesados.Add(new MensajeProcesado(messageId, routingKey));
            await _db.SaveChangesAsync();
        }
    }
}
