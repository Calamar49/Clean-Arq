using System.Net;
using System.Net.Http.Json;
using Reservas.Application.DTOs;
using Reservas.Application.Exceptions;
using Reservas.Application.Interfaces;

namespace Reservas.Infrastructure.Clients
{
    // Consulta sincrónica al servicio de Habitaciones mediante su API REST.
    // Reservas no accede nunca a la base de datos de ese servicio.
    public class HabitacionesClient : IHabitacionesClient
    {
        private readonly HttpClient _http;

        public HabitacionesClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<HabitacionInfoDTO?> ObtenerHabitacionAsync(int habitacionId)
        {
            try
            {
                var respuesta = await _http.GetAsync($"api/habitaciones/{habitacionId}");

                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                    return null;

                respuesta.EnsureSuccessStatusCode();

                return await respuesta.Content.ReadFromJsonAsync<HabitacionInfoDTO>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // El servicio no responde: se informa en lugar de fallar con un error genérico.
                throw new ServicioNoDisponibleException(
                    "El servicio de Habitaciones no está disponible. Intente nuevamente.");
            }
        }
    }
}
