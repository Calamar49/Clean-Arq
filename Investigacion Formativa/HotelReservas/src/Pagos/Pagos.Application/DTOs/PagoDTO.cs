namespace Pagos.Application.DTOs
{
    public class PagoDTO
    {
        public Guid Id { get; set; }
        public Guid ReservaId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Pasarela { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public string? Motivo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
