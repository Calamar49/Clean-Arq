using ClienteProductosMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ClienteProductosMVC.Controllers
{
    public class ProductosController : Controller
    {
        // Fábrica de HttpClient para consumir la API
        private readonly IHttpClientFactory _httpClientFactory;

        // Constructor del controlador
        public ProductosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ---------------------------------------------------------
        // GET: /Productos/Index
        // Muestra la lista de productos
        // ---------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            // Crea una instancia de cliente HTTP
            var client = _httpClientFactory.CreateClient("ProductosAPI");

            // Llama al endpoint GET de la API
            var productos = await
                client.GetFromJsonAsync<List<ProductoViewModel>>("api/productos");

            // Si la API devolviera null, se evita error en la vista
            productos ??= new List<ProductoViewModel>();

            // Envía la lista a la vista
            return View(productos);
        }

        // ---------------------------------------------------------
        // GET: /Productos/Create
        // Muestra el formulario para crear producto
        // ---------------------------------------------------------
        public IActionResult Create()
        {
            return View();
        }

        // ---------------------------------------------------------
        // POST: /Productos/Create
        // Envía los datos del formulario a la API
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create(ProductoViewModel model)
        {
            // Validación básica del lado MVC
            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                ModelState.AddModelError("", "El nombre es obligatorio");
            }

            if (model.Precio <= 0)
            {
                ModelState.AddModelError("", "El precio debe ser mayor a cero");
            }

            if (model.Stock < 0)
            {
                ModelState.AddModelError("", "El stock no puede ser negativo");
            }

            // Si hay errores, vuelve a mostrar la vista
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("ProductosAPI");

            // Envía el producto a la API mediante POST
            var response = await client.PostAsJsonAsync("api/productos", model);

            // Si la respuesta no fue exitosa, mostrar error simple
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "No se pudo registrar el producto en la API");
                return View(model);
            }

            // Si todo fue correcto, vuelve al listado
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------
        // GET: /Productos/Edit/5
        // Muestra los datos del producto a editar
        // ---------------------------------------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient("ProductosAPI");

            // Consulta el producto por id
            var producto = await
                client.GetFromJsonAsync<ProductoViewModel>($"api/productos/{id}");

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // ---------------------------------------------------------
        // POST: /Productos/Edit/5
        // Envía la actualización a la API
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                ModelState.AddModelError("", "El nombre es obligatorio");
            }

            if (model.Precio <= 0)
            {
                ModelState.AddModelError("", "El precio debe ser mayor a cero");
            }

            if (model.Stock < 0)
            {
                ModelState.AddModelError("", "El stock no puede ser negativo");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("ProductosAPI");

            // Envía datos actualizados mediante PUT
            var response = await client.PutAsJsonAsync($"api/productos/{id}", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "No se pudo actualizar el producto");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------
        // GET: /Productos/Delete/5
        // Muestra una vista de confirmación para eliminar
        // ---------------------------------------------------------
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("ProductosAPI");

            var producto = await
                client.GetFromJsonAsync<ProductoViewModel>($"api/productos/{id}");

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // ---------------------------------------------------------
        // POST: /Productos/Delete/5
        // Confirma la eliminación enviando DELETE a la API
        // ---------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmado(int id)
        {
            var client = _httpClientFactory.CreateClient("ProductosAPI");

            var response = await client.DeleteAsync($"api/productos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("No se pudo eliminar el producto");
            }

            return RedirectToAction("Index");
        }
    }
}
