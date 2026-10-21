using Habitaciones.Application.DTOs;

namespace Habitaciones.Application.Interfaces
{
    // Casos de uso que Application ofrece a la capa de presentación.
    public interface IHabitacionService
    {
        Task<List<HabitacionDTO>> ObtenerTodasAsync();

        Task<HabitacionDTO> ObtenerPorIdAsync(int id);

        Task<HabitacionDTO> CrearAsync(CrearHabitacionDTO dto);

        Task<HabitacionDTO> ActualizarAsync(int id, ActualizarHabitacionDTO dto);

        Task<List<HabitacionDTO>> ObtenerDisponiblesAsync(
            DateOnly entrada, DateOnly salida, int? capacidad);

        Task<ResultadoBloqueo> BloquearNochesAsync(BloquearNochesDTO dto);

        Task<int> LiberarReservaAsync(Guid reservaId);
    }
}
