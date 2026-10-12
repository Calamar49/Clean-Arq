using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicioProductosAPI.Data;
using ServicioProductosAPI.Models;

namespace ServicioProductosAPI.Controllers
{
    // Indica que esta clase funcionará como controlador de API
    [ApiController]

    // Define la ruta base del controlador.
    // En este caso: /api/productos
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        // Contexto de base de datos inyectado por dependencia
        private readonly AppDbContext _context;

        // Constructor del controlador
        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // GET: api/productos
        // Lista todos los productos
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            // Obtiene todos los productos desde la base de datos
            var productos = await _context.Productos.ToListAsync();

            // Retorna respuesta HTTP 200 OK con los datos en formato JSON
            return Ok(productos);
        }

        // ---------------------------------------------------------
        // GET: api/productos/5
        // Busca un producto por su Id
        // ---------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            // Busca el producto por clave primaria
            var producto = await _context.Productos.FindAsync(id);

            // Si no existe, devuelve 404
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            // Si existe, devuelve 200 OK con el producto
            return Ok(producto);
        }

        // ---------------------------------------------------------
        // POST: api/productos
        // Registra un nuevo producto
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Crear(Producto producto)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(producto.Nombre))
            {
                return BadRequest(new { mensaje = "El nombre es obligatorio" });
            }

            if (producto.Precio <= 0)
            {
                return BadRequest(new { mensaje = "El precio debe ser mayor a cero" });
            }

            if (producto.Stock < 0)
            {
                return BadRequest(new { mensaje = "El stock no puede ser negativo" });
            }

            // Agrega el producto al contexto
            _context.Productos.Add(producto);

            // Guarda los cambios en la base de datos
            await _context.SaveChangesAsync();

            // Devuelve respuesta exitosa con el producto creado
            return Ok(new
            {
                mensaje = "Producto registrado correctamente",
                producto = producto
            });
        }

        // ---------------------------------------------------------
        // PUT: api/productos/5
        // Actualiza un producto existente
        // ---------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, Producto productoActualizado)
        {
            // Busca el producto original en la base de datos
            var producto = await _context.Productos.FindAsync(id);

            // Si no existe, devuelve 404
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(productoActualizado.Nombre))
            {
                return BadRequest(new { mensaje = "El nombre es obligatorio" });
            }

            if (productoActualizado.Precio <= 0)
            {
                return BadRequest(new { mensaje = "El precio debe ser mayor a cero" });
            }

            if (productoActualizado.Stock < 0)
            {
                return BadRequest(new { mensaje = "El stock no puede ser negativo" });
            }

            // Actualiza los campos del producto
            producto.Nombre = productoActualizado.Nombre;
            producto.Categoria = productoActualizado.Categoria;
            producto.Precio = productoActualizado.Precio;
            producto.Stock = productoActualizado.Stock;

            // Guarda cambios
            await _context.SaveChangesAsync();

            // Respuesta exitosa
            return Ok(new
            {
                mensaje = "Producto actualizado correctamente",
                producto = producto
            });
        }

        // ---------------------------------------------------------
        // DELETE: api/productos/5
        // Elimina un producto por Id
        // ---------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            // Busca el producto
            var producto = await _context.Productos.FindAsync(id);

            // Si no existe, devuelve 404
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            // Elimina el producto del contexto
            _context.Productos.Remove(producto);

            // Guarda cambios en la base de datos
            await _context.SaveChangesAsync();

            // Devuelve confirmación
            return Ok(new { mensaje = "Producto eliminado correctamente" });
        }
    }
}
