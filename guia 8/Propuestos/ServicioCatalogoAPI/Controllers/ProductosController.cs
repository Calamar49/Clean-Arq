using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicioCatalogoAPI.Data;
using ServicioCatalogoAPI.Models;

namespace ServicioCatalogoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        // Valida las reglas de negocio de un producto.
        // Devuelve el mensaje de error, o null si es válido.
        private static string? Validar(Producto p)
        {
            if (string.IsNullOrWhiteSpace(p.Codigo))
                return "El código es obligatorio";

            if (p.Codigo.Length > 20)
                return "El código no puede superar los 20 caracteres";

            if (string.IsNullOrWhiteSpace(p.Nombre))
                return "El nombre es obligatorio";

            if (string.IsNullOrWhiteSpace(p.Categoria))
                return "La categoría es obligatoria";

            if (p.Precio <= 0)
                return "El precio debe ser mayor a cero";

            if (p.Stock < 0)
                return "El stock no puede ser negativo";

            return null;
        }

        // GET: api/productos
        // Lista todos los productos
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var productos = await _context.Productos
                .OrderBy(p => p.Id)
                .ToListAsync();

            return Ok(productos);
        }

        // GET: api/productos/5
        // Busca un producto por su Id
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            return Ok(producto);
        }

        // POST: api/productos
        // Registra un nuevo producto
        [HttpPost]
        public async Task<IActionResult> Crear(Producto producto)
        {
            var error = Validar(producto);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            // El código del producto debe ser único
            var codigo = producto.Codigo.Trim();
            if (await _context.Productos.AnyAsync(p => p.Codigo == codigo))
            {
                return Conflict(new { mensaje = $"Ya existe un producto con el código {codigo}" });
            }

            producto.Id = 0;
            producto.Codigo = codigo;
            producto.Nombre = producto.Nombre.Trim();
            producto.Categoria = producto.Categoria.Trim();

            // La fecha de registro la asigna el servidor
            producto.FechaRegistro = DateTime.Now;

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Producto registrado correctamente",
                producto = producto
            });
        }

        // PUT: api/productos/5
        // Actualiza un producto existente
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, Producto productoActualizado)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            var error = Validar(productoActualizado);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            // El nuevo código no debe pertenecer a otro producto
            var codigo = productoActualizado.Codigo.Trim();
            if (await _context.Productos.AnyAsync(p => p.Codigo == codigo && p.Id != id))
            {
                return Conflict(new { mensaje = $"Ya existe otro producto con el código {codigo}" });
            }

            producto.Codigo = codigo;
            producto.Nombre = productoActualizado.Nombre.Trim();
            producto.Descripcion = productoActualizado.Descripcion;
            producto.Precio = productoActualizado.Precio;
            producto.Stock = productoActualizado.Stock;
            producto.Categoria = productoActualizado.Categoria.Trim();
            producto.Activo = productoActualizado.Activo;
            // FechaRegistro no se modifica: conserva la fecha original.

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Producto actualizado correctamente",
                producto = producto
            });
        }

        // DELETE: api/productos/5
        // Elimina un producto por Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Producto eliminado correctamente" });
        }
    }
}
