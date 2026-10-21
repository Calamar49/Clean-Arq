namespace Pagos.Domain.Entities
{
    public enum EstadoPago
    {
        Aprobado,
        Rechazado
    }

    // Registro del cobro asociado a una reserva.
    public class Pago
    {
        public Guid Id { get; private set; }
        public Guid ReservaId { get; private set; }
        public decimal Monto { get; private set; }
        public string Moneda { get; private set; } = string.Empty;
        public EstadoPago Estado { get; private set; }

        // Pasarela que atendió el cobro (Simulada, Stripe, ...).
        public string Pasarela { get; private set; } = string.Empty;

        // Identificador del cobro en la pasarela, para conciliación.
        public string? Referencia { get; private set; }

        public string? Motivo { get; private set; }
        public DateTime FechaCreacion { get; private set; }

        // Constructor requerido por EF Core.
        private Pago()
        {
        }

        private Pago(Guid reservaId, decimal monto, string moneda, string pasarela)
        {
            if (reservaId == Guid.Empty)
                throw new ArgumentException("La reserva es obligatoria.");

            if (monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");

            Id = Guid.NewGuid();
            ReservaId = reservaId;
            Monto = monto;
            Moneda = moneda.ToLowerInvariant();
            Pasarela = pasarela;
            FechaCreacion = DateTime.Now;
        }

        public static Pago Aprobado(
            Guid reservaId, decimal monto, string moneda, string pasarela, string referencia)
        {
            return new Pago(reservaId, monto, moneda, pasarela)
            {
                Estado = EstadoPago.Aprobado,
                Referencia = referencia
            };
        }

        public static Pago Rechazado(
            Guid reservaId, decimal monto, string moneda, string pasarela, string motivo)
        {
            return new Pago(reservaId, monto, moneda, pasarela)
            {
                Estado = EstadoPago.Rechazado,
                Motivo = motivo
            };
        }
    }
}
