namespace ServicioProductosAPI.Models
{
    // Esta clase representa la entidad Producto.
    // Cada objeto de esta clase corresponde a un registro
    // de la tabla Productos en la base de datos.
    public class Producto
    {
        // Identificador único del producto.
        // En MySQL está configurado como AUTO_INCREMENT.
        public int Id { get; set; }

        // Nombre comercial o descriptivo del producto.
        public string Nombre { get; set; } = string.Empty;

        // Categoría a la que pertenece el producto.
        // Ejemplo: Computo, Accesorios, Periféricos, etc.
        public string Categoria { get; set; } = string.Empty;

        // Precio unitario del producto.
        // Se usa decimal para evitar problemas de precisión monetaria.
        public decimal Precio { get; set; }

        // Cantidad disponible en stock.
        public int Stock { get; set; }
    }
}
