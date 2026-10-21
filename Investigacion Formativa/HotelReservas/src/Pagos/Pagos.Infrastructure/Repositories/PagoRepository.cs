using Microsoft.EntityFrameworkCore;
using Pagos.Application.Interfaces;
using Pagos.Domain.Entities;
using Pagos.Infrastructure.Persistence;

namespace Pagos.Infrastructure.Repositories
{
    public class PagoRepository : IPagoRepository
    {
        private readonly PagosDbContext _db;

        public PagoRepository(PagosDbContext db)
        {
            _db = db;
        }

        public Task<List<Pago>> ListarAsync(Guid? reservaId)
        {
            var consulta = _db.Pagos.AsNoTracking().AsQueryable();

            if (reservaId.HasValue)
                consulta = consulta.Where(p => p.ReservaId == reservaId.Value);

            return consulta.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        }

        public Task<Pago?> ObtenerPorReservaAsync(Guid reservaId)
        {
            return _db.Pagos.AsNoTracking().FirstOrDefaultAsync(p => p.ReservaId == reservaId);
        }

        public async Task AgregarAsync(Pago pago)
        {
            _db.Pagos.Add(pago);
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
