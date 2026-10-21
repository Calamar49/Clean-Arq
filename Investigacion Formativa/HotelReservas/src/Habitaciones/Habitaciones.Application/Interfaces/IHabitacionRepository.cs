using Habitaciones.Domain.Entities;

namespace Habitaciones.Application.Interfaces
{
    // Resultado de intentar bloquear las noches de una reserva.
    public enum ResultadoBloqueo
    {
        Bloqueado,
        NoDisponible
    }

    // Contrato de persistencia. Application no conoce cómo se almacenan los datos.
    public interface IHabitacionRepository
    {
        Task<List<Habitacion>> ListarAsync();

        Task<Habitacion?> ObtenerAsync(int id);

        Task<bool> ExisteNumeroAsync(string numero);

        Task AgregarAsync(Habitacion habitacion);

        Task GuardarCambiosAsync();

        // Habitaciones activas sin ninguna noche bloqueada en [entrada, salida).
        Task<List<Habitacion>> ListarDisponiblesAsync(DateOnly entrada, DateOnly salida, int? capacidad);

        // Bloquea de forma atómica todas las noches de la reserva.
        // Si alguna ya está ocupada, no se bloquea ninguna.
        Task<ResultadoBloqueo> BloquearNochesAsync(
            Guid reservaId, int habitacionId, DateOnly entrada, DateOnly salida);

        // Libera todas las noches bloqueadas por una reserva (compensación de la Saga).
        Task<int> LiberarReservaAsync(Guid reservaId);
    }
}
