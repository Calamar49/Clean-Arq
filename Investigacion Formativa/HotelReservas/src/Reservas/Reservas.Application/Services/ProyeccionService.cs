using Hotel.Contracts;
using Microsoft.Extensions.Logging;
using Reservas.Application.DTOs;
using Reservas.Application.Exceptions;
using Reservas.Application.Interfaces;

namespace Reservas.Application.Services
{
    // Mantiene actualizado el modelo de lectura a partir de los eventos de la Saga.
    //
    // Esta es la pieza que hace de CQRS algo real: el modelo de escritura y el
    // de lectura evolucionan por separado y se sincronizan mediante eventos.
    // La consistencia es eventual, con un desfase de milisegundos.
    public interface IProyeccionService
    {
        Task ProyectarSolicitudAsync(ReservaSolicitada evento);

        Task ProyectarEstadoAsync(Guid reservaId, string estado, string? motivo, DateTime fecha);

        Task<List<ReservaVistaDTO>> ConsultarAsync(string? email, string? estado);

        Task<ReservaVistaDTO> ObtenerAsync(Guid id);
    }

    public class ProyeccionService : IProyeccionService
    {
        private readonly IReservaVistaRepository _vista;
        private readonly ILogger<ProyeccionService> _log;

        public ProyeccionService(IReservaVistaRepository vista, ILogger<ProyeccionService> log)
        {
            _vista = vista;
            _log = log;
        }

        public async Task ProyectarSolicitudAsync(ReservaSolicitada evento)
        {
            var creada = await _vista.CrearAsync(new ReservaVistaDTO
            {
                Id = evento.ReservaId,
                HabitacionNumero = evento.HabitacionNumero,
                HuespedNombre = evento.HuespedNombre,
                HuespedEmail = evento.HuespedEmail,
                FechaEntrada = evento.FechaEntrada,
                FechaSalida = evento.FechaSalida,
                Noches = evento.FechaSalida.DayNumber - evento.FechaEntrada.DayNumber,
                MontoTotal = evento.MontoTotal,
                Estado = "Pendiente",
                FechaCreacion = evento.FechaEvento,
                FechaActualizacion = evento.FechaEvento
            });

            if (!creada)
            {
                _log.LogInformation(
                    "La reserva {ReservaId} ya estaba proyectada: no se duplica.", evento.ReservaId);
            }
        }

        public async Task ProyectarEstadoAsync(
            Guid reservaId, string estado, string? motivo, DateTime fecha)
        {
            var actualizada = await _vista.ActualizarEstadoAsync(reservaId, estado, motivo, fecha);

            if (!actualizada)
            {
                // El evento de cambio de estado llegó antes que el de creación.
                // Se deja fallar a propósito para que la cola de reintento lo
                // devuelva más tarde, cuando la fila ya exista.
                throw new InvalidOperationException(
                    $"La vista de la reserva {reservaId} aún no existe; se reintentará.");
            }
        }

        public Task<List<ReservaVistaDTO>> ConsultarAsync(string? email, string? estado) =>
            _vista.ConsultarAsync(email, estado);

        public async Task<ReservaVistaDTO> ObtenerAsync(Guid id)
        {
            return await _vista.ObtenerAsync(id)
                ?? throw new NoEncontradoException("Reserva no encontrada en el modelo de lectura.");
        }
    }
}
