namespace ClienteProductosMVC.Models
{
    // Esta clase representa el modelo que la aplicación MVC
    // utilizará para enviar y recibir datos desde la API.
    public class ProductoViewModel
    {
        // Identificador del producto
        public int Id { get; set; }

        // Nombre del producto
        public string Nombre { get; set; } = string.Empty;

        // Categoría del producto
        public string Categoria { get; set; } = string.Empty;

        // Precio del producto
        public decimal Precio { get; set; }

        // Stock disponible
        public int Stock { get; set; }
    }
}
