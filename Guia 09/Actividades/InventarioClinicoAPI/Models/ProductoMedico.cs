namespace InventarioClinicoAPI.Models
{
    // Modelo que representa un producto médico dentro del inventario.
    public class ProductoMedico
    {
        // Identificador único del producto.
        public int Id { get; set; }

        // Nombre del insumo o medicamento.
        public string Nombre { get; set; } = string.Empty;

        // Categoría del producto.
        public string Categoria { get; set; } = string.Empty;

        // Cantidad disponible en almacén.
        public int Stock { get; set; }

        // Precio referencial del producto.
        public decimal Precio { get; set; }
    }
}
