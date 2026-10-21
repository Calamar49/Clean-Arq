using System.ComponentModel.DataAnnotations;

namespace ClienteHotelMVC.Models
{
    // Habitación disponible, tal como la devuelve el servicio de Habitaciones.
    public class HabitacionVM
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public bool Activa { get; set; }
    }

    // Resumen de un tipo de habitación, calculado a partir del catálogo real.
    public class TipoHabitacionVM
    {
        public string Tipo { get; set; } = string.Empty;
        public int CapacidadMaxima { get; set; }
        public decimal PrecioDesde { get; set; }
        public int Cantidad { get; set; }

        public string Descripcion => Tipo switch
        {
            "Simple" => "Ideal para viajeros que buscan tranquilidad y buena ubicación.",
            "Doble" => "Espacio cómodo para dos personas, con vista a la ciudad.",
            _ => "Nuestra opción más amplia, pensada para familias o estancias largas."
        };
    }

    // Criterios de búsqueda de disponibilidad.
    public class BuscarDisponibilidadVM
    {
        // Tipos de habitación que ofrece el hotel. Se muestran en la página
        // inicial mientras el visitante aún no ha realizado una búsqueda.
        public List<TipoHabitacionVM> Tipos { get; set; } = new();

        [Required(ErrorMessage = "Indique la fecha de llegada.")]
        [DataType(DataType.Date)]
        [Display(Name = "Llegada")]
        public DateOnly? FechaEntrada { get; set; }

        [Required(ErrorMessage = "Indique la fecha de salida.")]
        [DataType(DataType.Date)]
        [Display(Name = "Salida")]
        public DateOnly? FechaSalida { get; set; }

        [Range(1, 10, ErrorMessage = "El número de huéspedes debe estar entre 1 y 10.")]
        [Display(Name = "Huéspedes")]
        public int Huespedes { get; set; } = 2;

        public List<HabitacionVM> Resultados { get; set; } = new();

        // Indica si la búsqueda ya se ejecutó, para distinguir
        // "sin resultados" de "aún no ha buscado".
        public bool Buscado { get; set; }

        public int Noches =>
            FechaEntrada.HasValue && FechaSalida.HasValue
                ? Math.Max(0, FechaSalida.Value.DayNumber - FechaEntrada.Value.DayNumber)
                : 0;
    }

    // Datos del huésped para confirmar la reserva.
    public class CrearReservaVM
    {
        public int HabitacionId { get; set; }

        [Display(Name = "Habitación")]
        public string HabitacionNumero { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public decimal PrecioPorNoche { get; set; }

        [Required(ErrorMessage = "Indique la fecha de llegada.")]
        [DataType(DataType.Date)]
        [Display(Name = "Llegada")]
        public DateOnly FechaEntrada { get; set; }

        [Required(ErrorMessage = "Indique la fecha de salida.")]
        [DataType(DataType.Date)]
        [Display(Name = "Salida")]
        public DateOnly FechaSalida { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(120, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string HuespedNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string HuespedEmail { get; set; } = string.Empty;

        public int Noches => Math.Max(0, FechaSalida.DayNumber - FechaEntrada.DayNumber);

        public decimal Total => PrecioPorNoche * Noches;
    }

    // Segundo paso del proceso de compra: el huésped revisa el importe
    // y elige con qué medio desea pagar.
    public class PagoVM
    {
        public int HabitacionId { get; set; }

        [Display(Name = "Habitación")]
        public string HabitacionNumero { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;
        public decimal PrecioPorNoche { get; set; }

        [DataType(DataType.Date)]
        public DateOnly FechaEntrada { get; set; }

        [DataType(DataType.Date)]
        public DateOnly FechaSalida { get; set; }

        public string HuespedNombre { get; set; } = string.Empty;
        public string HuespedEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione con qué tarjeta desea pagar.")]
        [Display(Name = "Medio de pago")]
        public string MetodoPago { get; set; } = string.Empty;

        public int Noches => Math.Max(0, FechaSalida.DayNumber - FechaEntrada.DayNumber);

        public decimal Total => PrecioPorNoche * Noches;
    }

    // Reserva tal como la expone el modelo de lectura (CQRS).
    public class ReservaVM
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

        // Clase de color asociada al estado, para mostrarlo de forma coherente.
        public string ClaseEstado => Estado switch
        {
            "Pendiente" => "estado-pendiente",
            "Bloqueada" => "estado-bloqueada",
            "Confirmada" => "estado-confirmada",
            "Rechazada" => "estado-rechazada",
            _ => "estado-cancelada"
        };

        public string DescripcionEstado => Estado switch
        {
            "Pendiente" => "Procesando su solicitud",
            "Bloqueada" => "Habitación reservada, procesando el pago",
            "Confirmada" => "Reserva confirmada",
            "Rechazada" => "No se pudo completar",
            _ => "Reserva cancelada"
        };

        // Mientras la Saga está en curso la página se actualiza sola.
        public bool EnProceso => Estado is "Pendiente" or "Bloqueada";

        public bool SePuedeCancelar => Estado is "Pendiente" or "Bloqueada" or "Confirmada";
    }

    // Consulta de reservas por correo.
    public class MisReservasVM
    {
        [Required(ErrorMessage = "Indique el correo con el que realizó la reserva.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        public List<ReservaVM> Reservas { get; set; } = new();

        public bool Buscado { get; set; }
    }
}
