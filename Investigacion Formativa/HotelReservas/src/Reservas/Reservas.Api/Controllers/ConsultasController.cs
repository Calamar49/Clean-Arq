using Microsoft.AspNetCore.Mvc;
using Reservas.Application.Services;

namespace Reservas.Api.Controllers
{
    // Lado de CONSULTA (query) de CQRS.
    //
    // Lee exclusivamente del modelo de lectura, que la proyección mantiene
    // actualizado a partir de los eventos. Está separado del controlador de
    // comandos para que la distinción quede explícita.
    [ApiController]
    [Route("api/consultas/reservas")]
    public class ConsultasController : ControllerBase
    {
        private readonly IProyeccionService _proyeccion;

        public ConsultasController(IProyeccionService proyeccion)
        {
            _proyeccion = proyeccion;
        }

        // GET: api/consultas/reservas?email=...&estado=Confirmada
        [HttpGet]
        public async Task<IActionResult> Consultar(
            [FromQuery] string? email, [FromQuery] string? estado)
        {
            return Ok(await _proyeccion.ConsultarAsync(email, estado));
        }

        // GET: api/consultas/reservas/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Obtener(Guid id)
        {
            return Ok(await _proyeccion.ObtenerAsync(id));
        }
    }
}
