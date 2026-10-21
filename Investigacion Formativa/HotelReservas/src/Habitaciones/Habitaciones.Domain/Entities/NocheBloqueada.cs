namespace Habitaciones.Domain.Entities
{
    // Una noche concreta de una habitación que quedó bloqueada por una reserva.
    // La combinación (HabitacionId, Fecha) es única en la base de datos.
    public class NocheBloqueada
    {
        public long Id { get; private set; }
        public int HabitacionId { get; private set; }
        public DateOnly Fecha { get; private set; }
        public Guid ReservaId { get; private set; }

        // Constructor requerido por EF Core.
        private NocheBloqueada()
        {
        }

        public NocheBloqueada(int habitacionId, DateOnly fecha, Guid reservaId)
        {
            if (habitacionId <= 0)
                throw new ArgumentException("La habitación es obligatoria.");

            if (reservaId == Guid.Empty)
                throw new ArgumentException("La reserva es obligatoria.");

            HabitacionId = habitacionId;
            Fecha = fecha;
            ReservaId = reservaId;
        }
    }
}
