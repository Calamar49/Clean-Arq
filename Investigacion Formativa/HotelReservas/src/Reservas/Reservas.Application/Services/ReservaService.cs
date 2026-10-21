using Hotel.Contracts;
using Reservas.Application.DTOs;
using Reservas.Application.Exceptions;
using Reservas.Application.Interfaces;
using Reservas.Domain.Entities;
using Reservas.Domain.Exceptions;

namespace Reservas.Application.Services
{
    // Coordina los casos de uso de reservas y orquesta su parte de la Saga.
    // Depende solo de interfaces: no conoce EF Core, HTTP ni RabbitMQ.
    public class ReservaService : IReservaService
    {
        // Motivo con el que se rechaza una reserva cuyo bloqueo venció.
        public const string MotivoVencimiento =
            "El pago no se completó a tiempo y la habitación volvió a quedar disponible.";

        private readonly IReservaRepository _repository;
        private readonly IHabitacionesClient _habitaciones;
        private readonly IEventPublisher _eventos;

        public ReservaService(
            IReservaRepository repository,
            IHabitacionesClient habitaciones,
            IEventPublisher eventos)
        {
            _repository = repository;
            _habitaciones = habitaciones;
            _eventos = eventos;
        }

        public async Task<ReservaDTO> CrearAsync(CrearReservaDTO dto)
        {
            // El precio y el número de habitación se consultan al servicio dueño
            // de ese dato, nunca se reciben desde el cliente.
            var habitacion = await _habitaciones.ObtenerHabitacionAsync(dto.HabitacionId)
                ?? throw new NoEncontradoException("La habitación indicada no existe.");

            if (!habitacion.Activa)
                throw new ReglaNegocioException("La habitación no está disponible para reservas.");

            if (!MetodosPago.EsValido(dto.MetodoPago))
                throw new ReglaNegocioException("El medio de pago indicado no es válido.");

            var reserva = new Reserva(
                habitacion.Id,
                habitacion.Numero,
                dto.HuespedNombre,
                dto.HuespedEmail,
                dto.FechaEntrada,
                dto.FechaSalida,
                habitacion.PrecioPorNoche,
                dto.MetodoPago);

            await _repository.AgregarAsync(reserva);

            // Inicio de la Saga: Habitaciones intentará bloquear las noches.
            // Nota: la publicación ocurre después de guardar, por lo que no es
            // atómica con la transacción. El patrón Outbox cerraría esa ventana.
            await _eventos.PublicarAsync(new ReservaSolicitada(
                reserva.Id,
                reserva.HabitacionId,
                reserva.HabitacionNumero,
                reserva.FechaEntrada,
                reserva.FechaSalida,
                reserva.MontoTotal,
                reserva.HuespedNombre,
                reserva.HuespedEmail,
                reserva.MetodoPago));

            return Mapear(reserva);
        }

        public async Task<ReservaDTO> ObtenerPorIdAsync(Guid id)
        {
            var reserva = await _repository.ObtenerAsync(id)
                ?? throw new NoEncontradoException("Reserva no encontrada.");

            return Mapear(reserva);
        }

        public async Task<List<ReservaDTO>> ListarAsync(string? email, string? estado)
        {
            if (!string.IsNullOrWhiteSpace(estado) &&
                !Enum.TryParse<EstadoReserva>(estado, true, out _))
            {
                throw new ReglaNegocioException(
                    $"Estado inválido. Valores admitidos: {string.Join(", ", Enum.GetNames<EstadoReserva>())}.");
            }

            var reservas = await _repository.ListarAsync(email, estado);
            return reservas.Select(Mapear).ToList();
        }

        // El huésped cancela. Habitaciones liberará las noches si las había bloqueado.
        public async Task<ReservaDTO> CancelarAsync(Guid id, CancelarReservaDTO dto)
        {
            var reserva = await AplicarTransicionAsync(id, r => r.Cancelar(dto.Motivo ?? string.Empty));

            await _eventos.PublicarAsync(new ReservaCancelada(
                reserva.Id, reserva.HuespedNombre, reserva.HuespedEmail,
                reserva.Motivo ?? "Cancelada por el huésped."));

            return Mapear(reserva);
        }

        // Habitaciones confirmó el bloqueo: la reserva avanza y se solicita el pago.
        public async Task<ReservaDTO> MarcarBloqueadaAsync(Guid id)
        {
            var reserva = await AplicarTransicionAsync(id, r => r.MarcarBloqueada());

            await _eventos.PublicarAsync(new ReservaBloqueada(
                reserva.Id, reserva.MontoTotal, reserva.HuespedEmail, reserva.MetodoPago));

            return Mapear(reserva);
        }

        // El pago fue aprobado: fin feliz de la Saga.
        public async Task<ReservaDTO> ConfirmarAsync(Guid id)
        {
            var reserva = await AplicarTransicionAsync(id, r => r.Confirmar());

            await _eventos.PublicarAsync(new ReservaConfirmada(
                reserva.Id, reserva.HabitacionNumero, reserva.HuespedNombre, reserva.HuespedEmail,
                reserva.FechaEntrada, reserva.FechaSalida, reserva.MontoTotal));

            return Mapear(reserva);
        }

        // Sin disponibilidad o pago fallido: se publica el rechazo para que
        // Habitaciones libere las noches (compensación de la Saga).
        public async Task<ReservaDTO> RechazarAsync(Guid id, string motivo)
        {
            var reserva = await AplicarTransicionAsync(id, r => r.Rechazar(motivo));

            await _eventos.PublicarAsync(new ReservaRechazada(
                reserva.Id, reserva.HuespedNombre, reserva.HuespedEmail, motivo));

            return Mapear(reserva);
        }

        // Red de seguridad de la Saga. La compensación habitual la dispara un
        // evento (el pago se rechazó); esta la dispara el paso del tiempo, para
        // el caso en que el evento nunca llegue porque Pagos está caído.
        //
        // Se reutiliza el mismo rechazo que usa la Saga, así que la liberación
        // de las noches viaja por el camino de siempre: ReservaRechazada.
        public async Task<int> VencerBloqueosAsync(TimeSpan limite, CancellationToken ct = default)
        {
            var corte = DateTime.Now - limite;
            var vencidas = await _repository.ListarBloqueadasAntesDeAsync(corte);

            var liberadas = 0;

            foreach (var id in vencidas)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await RechazarAsync(id, MotivoVencimiento);
                    liberadas++;
                }
                catch (ConflictoException)
                {
                    // La Saga resolvió la reserva entre la consulta y el rechazo:
                    // llegó el pago justo a tiempo. Se deja como está.
                }
                catch (TransicionInvalidaException)
                {
                    // Lo mismo, detectado por la propia entidad de dominio.
                }
            }

            return liberadas;
        }

        // Aplica un cambio de estado sobre la reserva y lo persiste.
        // Las transiciones inválidas las rechaza la propia entidad de dominio.
        private async Task<Reserva> AplicarTransicionAsync(Guid id, Action<Reserva> transicion)
        {
            var reserva = await _repository.ObtenerAsync(id)
                ?? throw new NoEncontradoException("Reserva no encontrada.");

            transicion(reserva);

            if (!await _repository.GuardarCambiosAsync())
            {
                throw new ConflictoException(
                    "La reserva fue modificada por otro proceso. Vuelva a intentarlo.");
            }

            return reserva;
        }

        private static ReservaDTO Mapear(Reserva reserva)
        {
            return new ReservaDTO
            {
                Id = reserva.Id,
                HabitacionId = reserva.HabitacionId,
                HabitacionNumero = reserva.HabitacionNumero,
                HuespedNombre = reserva.HuespedNombre,
                HuespedEmail = reserva.HuespedEmail,
                FechaEntrada = reserva.FechaEntrada,
                FechaSalida = reserva.FechaSalida,
                Noches = reserva.Noches,
                PrecioPorNoche = reserva.PrecioPorNoche,
                MontoTotal = reserva.MontoTotal,
                MetodoPago = reserva.MetodoPago,
                Estado = reserva.Estado.ToString(),
                Motivo = reserva.Motivo,
                FechaCreacion = reserva.FechaCreacion,
                FechaActualizacion = reserva.FechaActualizacion,
                Version = reserva.Version
            };
        }
    }
}
