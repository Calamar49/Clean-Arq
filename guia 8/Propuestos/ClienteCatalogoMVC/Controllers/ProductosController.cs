using ClienteCatalogoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClienteCatalogoMVC.Controllers
{
    public class ProductosController : Controller
    {
        // Fábrica de HttpClient para consumir la API
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Lee el campo "mensaje" que devuelve la API cuando rechaza una petición
        // para mostrárselo al usuario en pantalla.
        private static async Task<string> LeerMensajeAsync(HttpResponseMessage response, string porDefecto)
        {
            try
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("mensaje", out var mensaje))
                {
                    return mensaje.GetString() ?? porDefecto;
                }
            }
            catch (JsonException)
            {
            }

            return porDefecto;
        }

        // GET: /Productos/Index
        // Muestra la lista de productos
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("CatalogoAPI");

            var productos = await
                client.GetFromJsonAsync<List<ProductoViewModel>>("api/productos");

            productos ??= new List<ProductoViewModel>();

            return View(productos);
        }

        // GET: /Productos/Create
        // Muestra el formulario para registrar un producto
        public IActionResult Create()
        {
            return View(new ProductoViewModel());
        }

        // POST: /Productos/Create
        // Valida los datos y los envía a la API
        [HttpPost]
        public async Task<IActionResult> Create(ProductoViewModel model)
        {
            // Las validaciones de campos obligatorios y formato
            // se definen en las anotaciones de ProductoViewModel.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("CatalogoAPI");

            var response = await client.PostAsJsonAsync("api/productos", model);

            // Si la API rechaza el registro (por ejemplo, código duplicado),
            // se muestra su mensaje en pantalla.
            if (!response.IsSuccessStatusCode)
            {
                var mensaje = await LeerMensajeAsync(response, "No se pudo registrar el producto en la API");
                ModelState.AddModelError("", mensaje);
                return View(model);
            }

            return RedirectToAction("Index");
        }

        // GET: /Productos/Edit/5
        // Muestra los datos del producto a editar
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient("CatalogoAPI");

            var respuesta = await client.GetAsync($"api/productos/{id}");
            if (!respuesta.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var producto = await respuesta.Content.ReadFromJsonAsync<ProductoViewModel>();

            return View(producto);
        }

        // POST: /Productos/Edit/5
        // Envía la actualización a la API
        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("CatalogoAPI");

            var response = await client.PutAsJsonAsync($"api/productos/{id}", model);

            if (!response.IsSuccessStatusCode)
            {
                var mensaje = await LeerMensajeAsync(response, "No se pudo actualizar el producto");
                ModelState.AddModelError("", mensaje);
                return View(model);
            }

            return RedirectToAction("Index");
        }

        // GET: /Productos/Delete/5
        // Muestra una vista de confirmación para eliminar
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("CatalogoAPI");

            var respuesta = await client.GetAsync($"api/productos/{id}");
            if (!respuesta.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var producto = await respuesta.Content.ReadFromJsonAsync<ProductoViewModel>();

            return View(producto);
        }

        // POST: /Productos/Delete/5
        // Confirma la eliminación enviando DELETE a la API
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmado(int id)
        {
            var client = _httpClientFactory.CreateClient("CatalogoAPI");

            var response = await client.DeleteAsync($"api/productos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("No se pudo eliminar el producto");
            }

            return RedirectToAction("Index");
        }
    }
}
