namespace ServicioCatalogoAPI.Models
{
    // Entidad Producto del catálogo.
    // Cada objeto corresponde a un registro de la tabla Productos
    // de la base de datos db_catalogo.
    public class Producto
    {
        // Identificador interno (AUTO_INCREMENT en MySQL).
        public int Id { get; set; }

        // Código único del producto. Ejemplo: PRD-001
        public string Codigo { get; set; } = string.Empty;

        // Nombre comercial del producto.
        public string Nombre { get; set; } = string.Empty;

        // Descripción opcional del producto.
        public string? Descripcion { get; set; }

        // Precio unitario (decimal para evitar errores de precisión).
        public decimal Precio { get; set; }

        // Cantidad disponible en stock.
        public int Stock { get; set; }

        // Categoría a la que pertenece el producto.
        public string Categoria { get; set; } = string.Empty;

        // Fecha en la que se registró el producto.
        // La asigna el servidor al crear el registro.
        public DateTime FechaRegistro { get; set; }

        // Indica si el producto está activo en el catálogo.
        public bool Activo { get; set; } = true;
    }
}
