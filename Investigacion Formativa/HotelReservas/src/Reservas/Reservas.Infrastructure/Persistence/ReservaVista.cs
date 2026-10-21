namespace Reservas.Infrastructure.Persistence
{
    // Fila del modelo de lectura (tabla ReservasVista).
    //
    // No es una entidad de dominio: no tiene reglas de negocio, solo los datos
    // ya resueltos que la interfaz necesita mostrar.
    public class ReservaVista
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
