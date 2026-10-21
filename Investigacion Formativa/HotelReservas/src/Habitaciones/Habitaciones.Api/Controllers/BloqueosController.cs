using Habitaciones.Application.DTOs;
using Habitaciones.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Habitaciones.Api.Controllers
{
    // Operaciones sobre el bloqueo de noches. En la Saga las invoca el consumidor
    // de mensajes; se exponen también por HTTP para probar la concurrencia.
    [ApiController]
    [Route("api/bloqueos")]
    public class BloqueosController : ControllerBase
    {
        private readonly IHabitacionService _service;

        public BloqueosController(IHabitacionService service)
        {
            _service = service;
        }

        // POST: api/bloqueos
        // 200: noches bloqueadas. 409: alguna noche ya está ocupada.
        [HttpPost]
        public async Task<IActionResult> Bloquear(BloquearNochesDTO dto)
        {
            var resultado = await _service.BloquearNochesAsync(dto);

            if (resultado == ResultadoBloqueo.NoDisponible)
            {
                return Conflict(new
                {
                    mensaje = "La habitación no está disponible en alguna de las fechas solicitadas."
                });
            }

            return Ok(new { mensaje = "Noches bloqueadas correctamente.", dto.ReservaId });
        }

        // DELETE: api/bloqueos/{reservaId}
        // Libera las noches de una reserva (compensación de la Saga).
        [HttpDelete("{reservaId:guid}")]
        public async Task<IActionResult> Liberar(Guid reservaId)
        {
            var liberadas = await _service.LiberarReservaAsync(reservaId);
            return Ok(new { mensaje = "Reserva liberada.", nochesLiberadas = liberadas });
        }
    }
}
