using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Reservas.Application.DTOs;
using Reservas.Application.Interfaces;
using Reservas.Infrastructure.Persistence;

namespace Reservas.Infrastructure.Repositories
{
    public class ReservaVistaRepository : IReservaVistaRepository
    {
        private readonly ReservasDbContext _db;

        public ReservaVistaRepository(ReservasDbContext db)
        {
            _db = db;
        }

        public Task<List<ReservaVistaDTO>> ConsultarAsync(string? email, string? estado)
        {
            // Consulta directa sobre la vista: sin conversiones ni uniones.
            var consulta = _db.ReservasVista.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizado = email.Trim().ToLowerInvariant();
                consulta = consulta.Where(v => v.HuespedEmail == normalizado);
            }

            if (!string.IsNullOrWhiteSpace(estado))
                consulta = consulta.Where(v => v.Estado == estado);

            return consulta
                .OrderByDescending(v => v.FechaCreacion)
                .Select(v => Mapear(v))
                .ToListAsync();
        }

        public async Task<ReservaVistaDTO?> ObtenerAsync(Guid id)
        {
            var vista = await _db.ReservasVista.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            return vista is null ? null : Mapear(vista);
        }

        public async Task<bool> CrearAsync(ReservaVistaDTO dto)
        {
            _db.ReservasVista.Add(new ReservaVista
            {
                Id = dto.Id,
                HabitacionNumero = dto.HabitacionNumero,
                HuespedNombre = dto.HuespedNombre,
                HuespedEmail = dto.HuespedEmail,
                FechaEntrada = dto.FechaEntrada,
                FechaSalida = dto.FechaSalida,
                Noches = dto.Noches,
                MontoTotal = dto.MontoTotal,
                Estado = dto.Estado,
                Motivo = dto.Motivo,
                FechaCreacion = dto.FechaCreacion,
                FechaActualizacion = dto.FechaActualizacion
            });

            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // El evento se reentregó y la fila ya existe: es idempotente.
                _db.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<bool> ActualizarEstadoAsync(
            Guid id, string estado, string? motivo, DateTime fecha)
        {
            // Actualización directa, sin cargar la entidad en memoria.
            var filas = await _db.ReservasVista
                .Where(v => v.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.Estado, estado)
                    .SetProperty(v => v.Motivo, motivo)
                    .SetProperty(v => v.FechaActualizacion, fecha));

            return filas > 0;
        }

        private static ReservaVistaDTO Mapear(ReservaVista v) => new()
        {
            Id = v.Id,
            HabitacionNumero = v.HabitacionNumero,
            HuespedNombre = v.HuespedNombre,
            HuespedEmail = v.HuespedEmail,
            FechaEntrada = v.FechaEntrada,
            FechaSalida = v.FechaSalida,
            Noches = v.Noches,
            MontoTotal = v.MontoTotal,
            Estado = v.Estado,
            Motivo = v.Motivo,
            FechaCreacion = v.FechaCreacion,
            FechaActualizacion = v.FechaActualizacion
        };
    }
}
