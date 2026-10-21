namespace Notificaciones.Application.DTOs
{
    public class NotificacionDTO
    {
        public Guid Id { get; set; }
        public Guid ReservaId { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Cuerpo { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
    }
}
