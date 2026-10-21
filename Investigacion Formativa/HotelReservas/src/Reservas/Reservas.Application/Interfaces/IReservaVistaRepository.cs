using Reservas.Application.DTOs;

namespace Reservas.Application.Interfaces
{
    // Acceso al modelo de lectura. Está separado del repositorio de escritura
    // para que las consultas no dependan de las entidades del dominio.
    public interface IReservaVistaRepository
    {
        Task<List<ReservaVistaDTO>> ConsultarAsync(string? email, string? estado);

        Task<ReservaVistaDTO?> ObtenerAsync(Guid id);

        // Inserta la fila de la vista al solicitarse la reserva.
        // Devuelve false si ya existía (mensaje reentregado).
        Task<bool> CrearAsync(ReservaVistaDTO vista);

        // Refleja un cambio de estado en la vista.
        // Devuelve false si la fila aún no existe, lo que indica que el evento
        // llegó antes que el de creación y conviene reintentarlo.
        Task<bool> ActualizarEstadoAsync(Guid id, string estado, string? motivo, DateTime fecha);
    }
}
