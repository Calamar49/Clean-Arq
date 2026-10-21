using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClienteHotelMVC.Models;

namespace ClienteHotelMVC.Services
{
    // Error de negocio devuelto por una API, con el mensaje ya extraído
    // para poder mostrárselo al usuario.
    public class ApiException : Exception
    {
        public ApiException(string mensaje) : base(mensaje)
        {
        }
    }

    // Lee la disponibilidad y el catálogo del servicio de Habitaciones.
    public class HabitacionesApiClient
    {
        private readonly HttpClient _http;

        public HabitacionesApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<HabitacionVM>> BuscarDisponiblesAsync(
            DateOnly entrada, DateOnly salida, int capacidad)
        {
            var url = $"api/habitaciones/disponibles?entrada={entrada:yyyy-MM-dd}" +
                      $"&salida={salida:yyyy-MM-dd}&capacidad={capacidad}";

            var respuesta = await EnviarAsync(() => _http.GetAsync(url));

            return await respuesta.Content.ReadFromJsonAsync<List<HabitacionVM>>() ?? new();
        }

        public async Task<HabitacionVM?> ObtenerAsync(int id)
        {
            var respuesta = await EnviarAsync(() => _http.GetAsync($"api/habitaciones/{id}"));
            return await respuesta.Content.ReadFromJsonAsync<HabitacionVM>();
        }

        // Catálogo completo, usado para mostrar los tipos de habitación en la
        // página inicial. Si el servicio no responde se devuelve una lista
        // vacía: es contenido informativo y no debe impedir usar el buscador.
        public async Task<List<HabitacionVM>> ListarCatalogoAsync()
        {
            try
            {
                var respuesta = await _http.GetAsync("api/habitaciones");

                if (!respuesta.IsSuccessStatusCode)
                    return new();

                return await respuesta.Content.ReadFromJsonAsync<List<HabitacionVM>>() ?? new();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return new();
            }
        }

        private static async Task<HttpResponseMessage> EnviarAsync(
            Func<Task<HttpResponseMessage>> peticion) =>
            await ClienteHttpHelper.EnviarAsync(peticion, "el servicio de habitaciones");
    }

    // Crea y consulta reservas en el servicio de Reservas.
    // Las consultas usan el modelo de lectura (CQRS).
    public class ReservasApiClient
    {
        private readonly HttpClient _http;

        public ReservasApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<ReservaVM> CrearAsync(PagoVM modelo)
        {
            var cuerpo = new
            {
                habitacionId = modelo.HabitacionId,
                huespedNombre = modelo.HuespedNombre,
                huespedEmail = modelo.HuespedEmail,
                fechaEntrada = modelo.FechaEntrada.ToString("yyyy-MM-dd"),
                fechaSalida = modelo.FechaSalida.ToString("yyyy-MM-dd"),
                metodoPago = modelo.MetodoPago
            };

            var respuesta = await ClienteHttpHelper.EnviarAsync(
                () => _http.PostAsJsonAsync("api/reservas", cuerpo), "el servicio de reservas");

            return await respuesta.Content.ReadFromJsonAsync<ReservaVM>()
                ?? throw new ApiException("No se pudo leer la reserva creada.");
        }

        // Consulta el modelo de lectura. Devuelve null si la proyección
        // todavía no ha registrado la reserva (consistencia eventual).
        public async Task<ReservaVM?> ObtenerVistaAsync(Guid id)
        {
            var respuesta = await _http.GetAsync($"api/consultas/reservas/{id}");

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!respuesta.IsSuccessStatusCode)
                return null;

            return await respuesta.Content.ReadFromJsonAsync<ReservaVM>();
        }

        public async Task<List<ReservaVM>> ConsultarPorEmailAsync(string email)
        {
            var respuesta = await ClienteHttpHelper.EnviarAsync(
                () => _http.GetAsync($"api/consultas/reservas?email={Uri.EscapeDataString(email)}"),
                "el servicio de reservas");

            return await respuesta.Content.ReadFromJsonAsync<List<ReservaVM>>() ?? new();
        }

        public async Task CancelarAsync(Guid id, string motivo)
        {
            await ClienteHttpHelper.EnviarAsync(
                () => _http.PostAsJsonAsync($"api/reservas/{id}/cancelar", new { motivo }),
                "el servicio de reservas");
        }
    }

    // Centraliza el tratamiento de errores de las llamadas HTTP: traduce los
    // fallos técnicos en mensajes comprensibles para el usuario.
    internal static class ClienteHttpHelper
    {
        public static async Task<HttpResponseMessage> EnviarAsync(
            Func<Task<HttpResponseMessage>> peticion, string servicio)
        {
            HttpResponseMessage respuesta;

            try
            {
                respuesta = await peticion();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new ApiException(
                    $"No se pudo contactar con {servicio}. Compruebe que esté en ejecución.");
            }

            if (respuesta.IsSuccessStatusCode)
                return respuesta;

            throw new ApiException(await LeerMensajeAsync(respuesta));
        }

        private static async Task<string> LeerMensajeAsync(HttpResponseMessage respuesta)
        {
            try
            {
                var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

                if (json.TryGetProperty("mensaje", out var mensaje))
                    return mensaje.GetString() ?? "La operación no pudo completarse.";

                // Errores de validación del modelo generados por ASP.NET Core.
                if (json.TryGetProperty("errors", out var errores))
                {
                    var textos = errores.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(t => !string.IsNullOrWhiteSpace(t));

                    if (textos.Any())
                        return string.Join(" ", textos);
                }
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
            }

            return "La operación no pudo completarse.";
        }
    }
}
