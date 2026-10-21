using Microsoft.AspNetCore.Mvc;
using Notificaciones.Application.Interfaces;

namespace Notificaciones.Api.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _service;

        public NotificacionesController(INotificacionService service)
        {
            _service = service;
        }

        // GET: api/notificaciones?destinatario=...&reservaId=...
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] string? destinatario, [FromQuery] Guid? reservaId)
        {
            return Ok(await _service.ListarAsync(destinatario, reservaId));
        }
    }
}
