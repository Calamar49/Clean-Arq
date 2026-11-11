namespace StockCriticoAPI.Models
{
    // Producto médico vigilado por nivel de existencias.
    //
    // Estado es un valor derivado: no se almacena como dato independiente,
    // se recalcula a partir de Stock y StockMinimo cada vez que el producto
    // cambia. Así nunca puede quedar desincronizado de las cifras reales.
    public class ProductoCritico
    {
        // Identificador del producto.
        public int Id { get; set; }

        // Nombre del producto.
        public string Nombre { get; set; } = string.Empty;

        // Existencias actuales en almacén.
        public int Stock { get; set; }

        // Nivel mínimo por debajo del cual se considera crítico.
        public int StockMinimo { get; set; }

        // Estado calculado: Crítico, Alerta o Normal.
        public string Estado { get; set; } = string.Empty;
    }
}
