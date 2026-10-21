using Habitaciones.Application.Interfaces;
using Habitaciones.Domain.Entities;
using Habitaciones.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Habitaciones.Infrastructure.Repositories
{
    public class HabitacionRepository : IHabitacionRepository
    {
        private const int IntentosMaximos = 3;

        private readonly HabitacionesDbContext _db;

        public HabitacionRepository(HabitacionesDbContext db)
        {
            _db = db;
        }

        public Task<List<Habitacion>> ListarAsync()
        {
            return _db.Habitaciones.AsNoTracking().OrderBy(h => h.Numero).ToListAsync();
        }

        public Task<Habitacion?> ObtenerAsync(int id)
        {
            return _db.Habitaciones.FirstOrDefaultAsync(h => h.Id == id);
        }

        public Task<bool> ExisteNumeroAsync(string numero)
        {
            return _db.Habitaciones.AnyAsync(h => h.Numero == numero);
        }

        public async Task AgregarAsync(Habitacion habitacion)
        {
            _db.Habitaciones.Add(habitacion);
            await _db.SaveChangesAsync();
        }

        public Task GuardarCambiosAsync()
        {
            return _db.SaveChangesAsync();
        }

        public Task<List<Habitacion>> ListarDisponiblesAsync(
            DateOnly entrada, DateOnly salida, int? capacidad)
        {
            // Noches ocupadas dentro del rango solicitado: [entrada, salida).
            var ocupadas = _db.NochesBloqueadas
                .Where(n => n.Fecha >= entrada && n.Fecha < salida)
                .Select(n => n.HabitacionId);

            return _db.Habitaciones
                .AsNoTracking()
                .Where(h => h.Activa
                    && !ocupadas.Contains(h.Id)
                    && (capacidad == null || h.Capacidad >= capacidad))
                .OrderBy(h => h.Numero)
                .ToListAsync();
        }

        public async Task<ResultadoBloqueo> BloquearNochesAsync(
            Guid reservaId, int habitacionId, DateOnly entrada, DateOnly salida)
        {
            for (var intento = 1; intento <= IntentosMaximos; intento++)
            {
                try
                {
                    // Idempotencia: si el mensaje se reentrega, la reserva ya está bloqueada.
                    if (await _db.NochesBloqueadas.AnyAsync(n => n.ReservaId == reservaId))
                        return ResultadoBloqueo.Bloqueado;

                    // Todas las noches se insertan en una sola transacción:
                    // o se bloquean todas o no se bloquea ninguna.
                    await using var transaccion = await _db.Database.BeginTransactionAsync();

                    for (var fecha = entrada; fecha < salida; fecha = fecha.AddDays(1))
                    {
                        _db.NochesBloqueadas.Add(new NocheBloqueada(habitacionId, fecha, reservaId));
                    }

                    await _db.SaveChangesAsync();
                    await transaccion.CommitAsync();

                    return ResultadoBloqueo.Bloqueado;
                }
                catch (DbUpdateException ex) when (EsClaveDuplicada(ex))
                {
                    // Otra reserva ya tomó alguna de las noches: lo detecta la
                    // restricción UNIQUE (HabitacionId, Fecha) de la base de datos.
                    _db.ChangeTracker.Clear();
                    return ResultadoBloqueo.NoDisponible;
                }
                catch (DbUpdateException ex) when (EsInterbloqueo(ex))
                {
                    // Dos transacciones se bloquearon entre sí: se reintenta.
                    _db.ChangeTracker.Clear();
                    await Task.Delay(50 * intento);
                }
            }

            return ResultadoBloqueo.NoDisponible;
        }

        public async Task<int> LiberarReservaAsync(Guid reservaId)
        {
            return await _db.NochesBloqueadas
                .Where(n => n.ReservaId == reservaId)
                .ExecuteDeleteAsync();
        }

        private static bool EsClaveDuplicada(DbUpdateException ex) =>
            ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };

        private static bool EsInterbloqueo(DbUpdateException ex) =>
            ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.LockDeadlock };
    }
}
