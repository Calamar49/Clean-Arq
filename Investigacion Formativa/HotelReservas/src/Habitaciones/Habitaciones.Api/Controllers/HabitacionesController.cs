using Habitaciones.Application.DTOs;
using Habitaciones.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Habitaciones.Api.Controllers
{
    [ApiController]
    [Route("api/habitaciones")]
    public class HabitacionesController : ControllerBase
    {
        private readonly IHabitacionService _service;

        public HabitacionesController(IHabitacionService service)
        {
            _service = service;
        }

        // GET: api/habitaciones
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            return Ok(await _service.ObtenerTodasAsync());
        }

        // GET: api/habitaciones/3
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            return Ok(await _service.ObtenerPorIdAsync(id));
        }

        // GET: api/habitaciones/disponibles?entrada=2026-12-20&salida=2026-12-23&capacidad=2
        [HttpGet("disponibles")]
        public async Task<IActionResult> Disponibles(
            [FromQuery] DateOnly entrada,
            [FromQuery] DateOnly salida,
            [FromQuery] int? capacidad)
        {
            return Ok(await _service.ObtenerDisponiblesAsync(entrada, salida, capacidad));
        }

        // POST: api/habitaciones
        [HttpPost]
        public async Task<IActionResult> Crear(CrearHabitacionDTO dto)
        {
            var creada = await _service.CrearAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creada.Id }, creada);
        }

        // PUT: api/habitaciones/3
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, ActualizarHabitacionDTO dto)
        {
            return Ok(await _service.ActualizarAsync(id, dto));
        }
    }
}
