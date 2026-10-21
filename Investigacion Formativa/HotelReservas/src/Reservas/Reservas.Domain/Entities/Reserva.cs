using Reservas.Domain.Exceptions;

namespace Reservas.Domain.Entities
{
    // Entidad principal del servicio de Reservas.
    // Concentra las reglas de la Saga: solo permite los cambios de estado válidos
    // y mantiene el token de concurrencia optimista.
    public class Reserva
    {
        public const int MaximoNoches = 30;

        public Guid Id { get; private set; }
        public int HabitacionId { get; private set; }
        public string HabitacionNumero { get; private set; } = string.Empty;
        public string HuespedNombre { get; private set; } = string.Empty;
        public string HuespedEmail { get; private set; } = string.Empty;
        public DateOnly FechaEntrada { get; private set; }
        public DateOnly FechaSalida { get; private set; }
        public int Noches { get; private set; }
        public decimal PrecioPorNoche { get; private set; }
        public decimal MontoTotal { get; private set; }

        // Identificador del medio de pago elegido por el huésped.
        // No contiene datos de tarjeta: solo la referencia que la pasarela
        // traducirá a su propio método.
        public string MetodoPago { get; private set; } = string.Empty;

        public EstadoReserva Estado { get; private set; }
        public string? Motivo { get; private set; }
        public DateTime FechaCreacion { get; private set; }
        public DateTime FechaActualizacion { get; private set; }

        // Token de concurrencia optimista: se incrementa en cada cambio de estado.
        // EF Core lo incluye en el WHERE del UPDATE, de modo que si otro proceso
        // modificó la reserva primero, esta actualización falla en lugar de pisarla.
        public int Version { get; private set; }

        // Constructor requerido por EF Core.
        private Reserva()
        {
        }

        public Reserva(
            int habitacionId,
            string habitacionNumero,
            string huespedNombre,
            string huespedEmail,
            DateOnly fechaEntrada,
            DateOnly fechaSalida,
            decimal precioPorNoche,
            string metodoPago)
        {
            if (habitacionId <= 0)
                throw new ArgumentException("La habitación es obligatoria.");

            if (string.IsNullOrWhiteSpace(metodoPago))
                throw new ArgumentException("Debe indicarse el medio de pago.");

            if (string.IsNullOrWhiteSpace(huespedNombre))
                throw new ArgumentException("El nombre del huésped es obligatorio.");

            if (string.IsNullOrWhiteSpace(huespedEmail) || !huespedEmail.Contains('@'))
                throw new ArgumentException("El correo del huésped no tiene un formato válido.");

            if (fechaSalida <= fechaEntrada)
                throw new ArgumentException("La fecha de salida debe ser posterior a la de entrada.");

            if (fechaEntrada < DateOnly.FromDateTime(DateTime.Today))
                throw new ArgumentException("La fecha de entrada no puede estar en el pasado.");

            var noches = fechaSalida.DayNumber - fechaEntrada.DayNumber;
            if (noches > MaximoNoches)
                throw new ArgumentException($"La estadía no puede superar las {MaximoNoches} noches.");

            if (precioPorNoche <= 0)
                throw new ArgumentException("El precio por noche debe ser mayor a cero.");

            Id = Guid.NewGuid();
            HabitacionId = habitacionId;
            HabitacionNumero = habitacionNumero.Trim();
            HuespedNombre = huespedNombre.Trim();
            HuespedEmail = huespedEmail.Trim().ToLowerInvariant();
            FechaEntrada = fechaEntrada;
            FechaSalida = fechaSalida;
            Noches = noches;
            PrecioPorNoche = precioPorNoche;
            MontoTotal = precioPorNoche * noches;
            MetodoPago = metodoPago.Trim();
            Estado = EstadoReserva.Pendiente;
            FechaCreacion = DateTime.Now;
            FechaActualizacion = FechaCreacion;
            Version = 1;
        }

        // Paso 1 de la Saga: el servicio de Habitaciones bloqueó las noches.
        public void MarcarBloqueada()
        {
            ExigirEstado(nameof(MarcarBloqueada), EstadoReserva.Pendiente);
            CambiarEstado(EstadoReserva.Bloqueada, null);
        }

        // Paso 2 de la Saga: el pago fue aprobado.
        public void Confirmar()
        {
            ExigirEstado(nameof(Confirmar), EstadoReserva.Bloqueada);
            CambiarEstado(EstadoReserva.Confirmada, null);
        }

        // Fin alternativo de la Saga: sin disponibilidad o pago fallido.
        // Desde Bloqueada implica compensación (liberar las noches).
        public void Rechazar(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Debe indicarse el motivo del rechazo.");

            ExigirEstado(nameof(Rechazar), EstadoReserva.Pendiente, EstadoReserva.Bloqueada);
            CambiarEstado(EstadoReserva.Rechazada, motivo.Trim());
        }

        // Cancelación solicitada por el huésped sobre una reserva ya confirmada.
        public void Cancelar(string motivo)
        {
            ExigirEstado(nameof(Cancelar), EstadoReserva.Pendiente, EstadoReserva.Bloqueada, EstadoReserva.Confirmada);

            var texto = string.IsNullOrWhiteSpace(motivo)
                ? "Cancelada por el huésped."
                : motivo.Trim();

            CambiarEstado(EstadoReserva.Cancelada, texto);
        }

        // Indica si la reserva mantiene noches bloqueadas que habría que liberar.
        public bool RequiereLiberarHabitacion() =>
            Estado is EstadoReserva.Bloqueada or EstadoReserva.Confirmada;

        private void CambiarEstado(EstadoReserva nuevo, string? motivo)
        {
            Estado = nuevo;
            Motivo = motivo;
            FechaActualizacion = DateTime.Now;
            Version++;
        }

        private void ExigirEstado(string operacion, params EstadoReserva[] permitidos)
        {
            if (!permitidos.Contains(Estado))
            {
                throw new TransicionInvalidaException(
                    $"No se puede ejecutar {operacion} sobre una reserva en estado {Estado}.");
            }
        }
    }
}
