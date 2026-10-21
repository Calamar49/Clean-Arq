using Microsoft.EntityFrameworkCore;
using Reservas.Application.Interfaces;
using Reservas.Domain.Entities;
using Reservas.Infrastructure.Persistence;

namespace Reservas.Infrastructure.Repositories
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly ReservasDbContext _db;

        public ReservaRepository(ReservasDbContext db)
        {
            _db = db;
        }

        public Task<Reserva?> ObtenerAsync(Guid id)
        {
            return _db.Reservas.FirstOrDefaultAsync(r => r.Id == id);
        }

        public Task<List<Reserva>> ListarAsync(string? email, string? estado)
        {
            var consulta = _db.Reservas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizado = email.Trim().ToLowerInvariant();
                consulta = consulta.Where(r => r.HuespedEmail == normalizado);
            }

            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoReserva>(estado, true, out var estadoFiltro))
            {
                consulta = consulta.Where(r => r.Estado == estadoFiltro);
            }

            return consulta.OrderByDescending(r => r.FechaCreacion).ToListAsync();
        }

        public Task<List<Guid>> ListarBloqueadasAntesDeAsync(DateTime limite, int maximo = 100)
        {
            // Se acota el lote para que una acumulación de reservas atascadas
            // no convierta la revisión periódica en una consulta pesada.
            return _db.Reservas
                .AsNoTracking()
                .Where(r => r.Estado == EstadoReserva.Bloqueada && r.FechaActualizacion < limite)
                .OrderBy(r => r.FechaActualizacion)
                .Take(maximo)
                .Select(r => r.Id)
                .ToListAsync();
        }

        public async Task AgregarAsync(Reserva reserva)
        {
            _db.Reservas.Add(reserva);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> GuardarCambiosAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otro proceso modificó la reserva entre la lectura y la escritura.
                _db.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
