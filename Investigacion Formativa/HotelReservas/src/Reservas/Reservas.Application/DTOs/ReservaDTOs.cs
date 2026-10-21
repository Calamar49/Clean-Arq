using System.ComponentModel.DataAnnotations;

namespace Reservas.Application.DTOs
{
    // Datos de salida de una reserva.
    public class ReservaDTO
    {
        public Guid Id { get; set; }
        public int HabitacionId { get; set; }
        public string HabitacionNumero { get; set; } = string.Empty;
        public string HuespedNombre { get; set; } = string.Empty;
        public string HuespedEmail { get; set; } = string.Empty;
        public DateOnly FechaEntrada { get; set; }
        public DateOnly FechaSalida { get; set; }
        public int Noches { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public decimal MontoTotal { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public int Version { get; set; }
    }

    // Datos de entrada para solicitar una reserva.
    public class CrearReservaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una habitación.")]
        public int HabitacionId { get; set; }

        [Required(ErrorMessage = "El nombre del huésped es obligatorio.")]
        [StringLength(120, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
        public string HuespedNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo del huésped es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        public string HuespedEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de entrada es obligatoria.")]
        public DateOnly FechaEntrada { get; set; }

        [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
        public DateOnly FechaSalida { get; set; }

        // Medio de pago elegido por el huésped (ver Hotel.Contracts.MetodosPago).
        [Required(ErrorMessage = "Debe seleccionar un medio de pago.")]
        public string MetodoPago { get; set; } = string.Empty;
    }

    // Datos de entrada para cancelar una reserva.
    public class CancelarReservaDTO
    {
        [StringLength(250)]
        public string? Motivo { get; set; }
    }

    // Datos de una habitación consultados al servicio de Habitaciones.
    public class HabitacionInfoDTO
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public bool Activa { get; set; }
    }
}
