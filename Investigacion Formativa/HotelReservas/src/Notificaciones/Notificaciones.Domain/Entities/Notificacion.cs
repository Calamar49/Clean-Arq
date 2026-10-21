namespace Notificaciones.Domain.Entities
{
    public enum TipoNotificacion
    {
        ReservaConfirmada,
        ReservaRechazada,
        ReservaCancelada
    }

    // Aviso generado para el huésped.
    // El envío de correo se simula: se registra el mensaje que se habría enviado.
    public class Notificacion
    {
        public Guid Id { get; private set; }
        public Guid ReservaId { get; private set; }
        public string Destinatario { get; private set; } = string.Empty;
        public TipoNotificacion Tipo { get; private set; }
        public string Asunto { get; private set; } = string.Empty;
        public string Cuerpo { get; private set; } = string.Empty;
        public DateTime FechaEnvio { get; private set; }

        private Notificacion()
        {
        }

        public Notificacion(
            Guid reservaId, string destinatario, TipoNotificacion tipo, string asunto, string cuerpo)
        {
            if (reservaId == Guid.Empty)
                throw new ArgumentException("La reserva es obligatoria.");

            if (string.IsNullOrWhiteSpace(destinatario))
                throw new ArgumentException("El destinatario es obligatorio.");

            Id = Guid.NewGuid();
            ReservaId = reservaId;
            Destinatario = destinatario.Trim().ToLowerInvariant();
            Tipo = tipo;
            Asunto = asunto;
            Cuerpo = cuerpo;
            FechaEnvio = DateTime.Now;
        }
    }
}
