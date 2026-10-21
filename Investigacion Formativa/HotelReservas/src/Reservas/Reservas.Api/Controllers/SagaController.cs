using Microsoft.AspNetCore.Mvc;
using Reservas.Application.Interfaces;

namespace Reservas.Api.Controllers
{
    // Transiciones de estado que ejecuta la Saga.
    // A partir de la fase de mensajería las dispara el consumidor de eventos;
    // se exponen por HTTP para poder probar el flujo de forma aislada.
    [ApiController]
    [Route("api/saga/reservas")]
    public class SagaController : ControllerBase
    {
        private readonly IReservaService _service;

        public SagaController(IReservaService service)
        {
            _service = service;
        }

        public record MotivoDTO(string Motivo);

        // El servicio de Habitaciones bloqueó las noches solicitadas.
        [HttpPost("{id:guid}/bloqueada")]
        public async Task<IActionResult> MarcarBloqueada(Guid id)
        {
            return Ok(await _service.MarcarBloqueadaAsync(id));
        }

        // El pago fue aprobado: la reserva queda en firme.
        [HttpPost("{id:guid}/confirmar")]
        public async Task<IActionResult> Confirmar(Guid id)
        {
            return Ok(await _service.ConfirmarAsync(id));
        }

        // No hubo disponibilidad o el pago falló.
        [HttpPost("{id:guid}/rechazar")]
        public async Task<IActionResult> Rechazar(Guid id, MotivoDTO dto)
        {
            return Ok(await _service.RechazarAsync(id, dto.Motivo));
        }
    }
}
