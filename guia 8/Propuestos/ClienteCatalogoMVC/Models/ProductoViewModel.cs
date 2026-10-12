using System.ComponentModel.DataAnnotations;

namespace ClienteCatalogoMVC.Models
{
    // Modelo que la aplicación MVC usa para enviar y recibir
    // los datos de un producto desde la API.
    // Las anotaciones definen las validaciones de campos obligatorios
    // y de formato, con mensajes de error para el usuario.
    public class ProductoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres")]
        [RegularExpression(@"^[A-Z]{3}-\d{3}$",
            ErrorMessage = "El código debe tener el formato PRD-001 (3 letras mayúsculas, guion y 3 dígitos)")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres")]
        public string? Descripcion { get; set; }

        [Range(typeof(decimal), "0.01", "99999999.99",
            ErrorMessage = "El precio debe ser mayor a cero")]
        public decimal Precio { get; set; }

        [Range(0, 1000000, ErrorMessage = "El stock debe ser un número entero mayor o igual a cero")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [StringLength(100, ErrorMessage = "La categoría no puede superar los 100 caracteres")]
        public string Categoria { get; set; } = string.Empty;

        // La asigna la API al registrar el producto.
        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; } = true;
    }
}
