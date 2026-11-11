using Microsoft.AspNetCore.Mvc;
using InventarioClinicoAPI.Models;

namespace InventarioClinicoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        // Lista simulada de productos médicos.
        // En esta guía no usamos base de datos todavía.
        // El objetivo es centrarnos en Docker y en la creación de imágenes.
        private static readonly List<ProductoMedico> productos = new()
        {
            new ProductoMedico
            {
                Id = 1,
                Nombre = "Mascarilla KN95",
                Categoria = "Protección",
                Stock = 250,
                Precio = 3.50m
            },
            new ProductoMedico
            {
                Id = 2,
                Nombre = "Guantes de látex",
                Categoria = "Protección",
                Stock = 500,
                Precio = 1.20m
            },
            new ProductoMedico
            {
                Id = 3,
                Nombre = "Alcohol medicinal",
                Categoria = "Desinfección",
                Stock = 180,
                Precio = 7.00m
            },
            new ProductoMedico
            {
                Id = 4,
                Nombre = "Jeringa 10 ml",
                Categoria = "Procedimiento",
                Stock = 320,
                Precio = 0.80m
            }
        };

        // Devuelve la lista completa del inventario.
        [HttpGet]
        public ActionResult<IEnumerable<ProductoMedico>> Get()
        {
            return Ok(productos);
        }

        // Devuelve un producto específico según su Id.
        [HttpGet("{id}")]
        public ActionResult<ProductoMedico> GetById(int id)
        {
            var producto = productos.FirstOrDefault(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new
                {
                    mensaje = "Producto no encontrado."
                });
            }

            return Ok(producto);
        }
    }
}
