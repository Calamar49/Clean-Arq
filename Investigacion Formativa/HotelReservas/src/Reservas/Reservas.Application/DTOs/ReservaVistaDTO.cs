namespace Reservas.Application.DTOs
{
    // Proyección de lectura de una reserva (lado "query" de CQRS).
    //
    // Es una vista plana y denormalizada: contiene ya resueltos los datos que
    // la interfaz necesita mostrar, sin tener que consultar el modelo de
    // escritura ni convertir enumeraciones.
    public class ReservaVistaDTO
    {
        public Guid Id { get; set; }
        public string HabitacionNumero { get; set; } = string.Empty;
        public string HuespedNombre { get; set; } = string.Empty;
        public string HuespedEmail { get; set; } = string.Empty;
        public DateOnly FechaEntrada { get; set; }
        public DateOnly FechaSalida { get; set; }
        public int Noches { get; set; }
        public decimal MontoTotal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
