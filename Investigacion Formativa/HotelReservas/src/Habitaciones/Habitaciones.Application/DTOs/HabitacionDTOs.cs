using System.ComponentModel.DataAnnotations;

namespace Habitaciones.Application.DTOs
{
    // Datos de salida de una habitación.
    public class HabitacionDTO
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public bool Activa { get; set; }
    }

    // Datos de entrada para registrar una habitación.
    public class CrearHabitacionDTO
    {
        [Required(ErrorMessage = "El número es obligatorio.")]
        [StringLength(10)]
        public string Numero { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo es obligatorio (Simple, Doble o Suite).")]
        public string Tipo { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "La capacidad debe estar entre 1 y 10.")]
        public int Capacidad { get; set; }

        [Range(0.01, 100000, ErrorMessage = "El precio debe ser mayor a cero.")]
        public decimal PrecioPorNoche { get; set; }
    }

    // Datos de entrada para modificar una habitación.
    public class ActualizarHabitacionDTO
    {
        [Required(ErrorMessage = "El tipo es obligatorio (Simple, Doble o Suite).")]
        public string Tipo { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "La capacidad debe estar entre 1 y 10.")]
        public int Capacidad { get; set; }

        [Range(0.01, 100000, ErrorMessage = "El precio debe ser mayor a cero.")]
        public decimal PrecioPorNoche { get; set; }

        public bool Activa { get; set; } = true;
    }

    // Solicitud para bloquear las noches de una reserva.
    public class BloquearNochesDTO
    {
        public Guid ReservaId { get; set; }
        public int HabitacionId { get; set; }
        public DateOnly FechaEntrada { get; set; }
        public DateOnly FechaSalida { get; set; }
    }
}
