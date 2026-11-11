using Microsoft.AspNetCore.Mvc;
using StockCriticoAPI.Models;
using StockCriticoAPI.Services;

namespace StockCriticoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        // Lista en memoria con los tres estados representados, para que la
        // comprobación desde el contenedor muestre algo útil de entrada.
        private static readonly List<ProductoCritico> productos = new()
        {
            new ProductoCritico { Id = 1, Nombre = "Mascarilla KN95",    Stock = 250, StockMinimo = 100 },
            new ProductoCritico { Id = 2, Nombre = "Alcohol medicinal",  Stock =  40, StockMinimo =  60 },
            new ProductoCritico { Id = 3, Nombre = "Guantes de látex",   Stock =  80, StockMinimo =  80 },
            new ProductoCritico { Id = 4, Nombre = "Jeringa 10 ml",      Stock =  15, StockMinimo =  50 }
        };

        // El estado se recalcula al arrancar para que los datos de ejemplo
        // nunca contradigan la regla.
        static StockController()
        {
            foreach (var producto in productos)
            {
                producto.Estado = EvaluadorStock.Evaluar(producto);
            }
        }

        // Lista todos los productos vigilados.
        [HttpGet]
        public ActionResult<IEnumerable<ProductoCritico>> Get()
        {
            return Ok(productos);
        }

        // Devuelve únicamente los productos en estado crítico.
        [HttpGet("criticos")]
        public ActionResult<IEnumerable<ProductoCritico>> GetCriticos()
        {
            var criticos = productos
                .Where(p => p.Estado == EvaluadorStock.Critico)
                .OrderBy(p => p.Stock);

            return Ok(criticos);
        }

        // Registra un producto y calcula su estado automáticamente.
        [HttpPost]
        public ActionResult<ProductoCritico> Post([FromBody] ProductoCritico nuevoProducto)
        {
            if (string.IsNullOrWhiteSpace(nuevoProducto.Nombre))
            {
                return BadRequest(new
                {
                    mensaje = "El nombre del producto es obligatorio."
                });
            }

            if (nuevoProducto.Stock < 0)
            {
                return BadRequest(new
                {
                    mensaje = "El stock no puede ser negativo."
                });
            }

            if (nuevoProducto.StockMinimo < 0)
            {
                return BadRequest(new
                {
                    mensaje = "El stock mínimo no puede ser negativo."
                });
            }

            nuevoProducto.Id = productos.Count == 0 ? 1 : productos.Max(p => p.Id) + 1;

            // Se ignora cualquier estado enviado por el cliente: es un valor
            // derivado, no un dato de entrada.
            nuevoProducto.Estado = EvaluadorStock.Evaluar(nuevoProducto);

            productos.Add(nuevoProducto);

            return CreatedAtAction(nameof(Get), new { id = nuevoProducto.Id }, nuevoProducto);
        }
    }
}
