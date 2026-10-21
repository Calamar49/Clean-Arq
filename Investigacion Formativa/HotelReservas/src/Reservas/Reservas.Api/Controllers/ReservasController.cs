using Microsoft.AspNetCore.Mvc;
using Reservas.Application.DTOs;
using Reservas.Application.Interfaces;

namespace Reservas.Api.Controllers
{
    [ApiController]
    [Route("api/reservas")]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaService _service;

        public ReservasController(IReservaService service)
        {
            _service = service;
        }

        // GET: api/reservas?email=...&estado=Confirmada
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] string? email, [FromQuery] string? estado)
        {
            return Ok(await _service.ListarAsync(email, estado));
        }

        // GET: api/reservas/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObtenerPorId(Guid id)
        {
            return Ok(await _service.ObtenerPorIdAsync(id));
        }

        // POST: api/reservas
        // Registra la solicitud en estado Pendiente e inicia la Saga.
        [HttpPost]
        public async Task<IActionResult> Crear(CrearReservaDTO dto)
        {
            var reserva = await _service.CrearAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = reserva.Id }, reserva);
        }

        // POST: api/reservas/{id}/cancelar
        [HttpPost("{id:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id, CancelarReservaDTO dto)
        {
            return Ok(await _service.CancelarAsync(id, dto));
        }
    }
}
