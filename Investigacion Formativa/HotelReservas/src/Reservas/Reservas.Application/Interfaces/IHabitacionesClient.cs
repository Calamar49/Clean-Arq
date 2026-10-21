using Reservas.Application.DTOs;

namespace Reservas.Application.Interfaces
{
    // Acceso de solo lectura al servicio de Habitaciones.
    // Reservas nunca consulta la base de datos de otro servicio: usa su API.
    public interface IHabitacionesClient
    {
        // Devuelve los datos de la habitación, o null si no existe.
        Task<HabitacionInfoDTO?> ObtenerHabitacionAsync(int habitacionId);
    }
}
