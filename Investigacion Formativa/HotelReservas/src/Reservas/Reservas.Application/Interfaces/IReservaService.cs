using Reservas.Application.DTOs;

namespace Reservas.Application.Interfaces
{
    // Casos de uso del servicio de Reservas (lado de comandos).
    public interface IReservaService
    {
        Task<ReservaDTO> CrearAsync(CrearReservaDTO dto);

        Task<ReservaDTO> ObtenerPorIdAsync(Guid id);

        Task<List<ReservaDTO>> ListarAsync(string? email, string? estado);

        Task<ReservaDTO> CancelarAsync(Guid id, CancelarReservaDTO dto);

        // Transiciones que ejecuta la Saga al recibir los eventos
        // de los servicios de Habitaciones y Pagos.
        Task<ReservaDTO> MarcarBloqueadaAsync(Guid id);

        Task<ReservaDTO> ConfirmarAsync(Guid id);

        Task<ReservaDTO> RechazarAsync(Guid id, string motivo);

        // Rechaza las reservas que llevan bloqueadas más tiempo del permitido,
        // de modo que sus noches vuelvan a estar disponibles. Devuelve cuántas
        // se liberaron.
        Task<int> VencerBloqueosAsync(TimeSpan limite, CancellationToken ct = default);
    }
}
