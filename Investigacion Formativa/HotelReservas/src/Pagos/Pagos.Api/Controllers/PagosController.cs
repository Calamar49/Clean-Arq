using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pagos.Application.Configuracion;
using Pagos.Application.Interfaces;

namespace Pagos.Api.Controllers
{
    [ApiController]
    [Route("api/pagos")]
    public class PagosController : ControllerBase
    {
        private readonly IPagoService _service;
        private readonly PasarelaOptions _opciones;
        private readonly IPasarelaPago _pasarela;

        public PagosController(
            IPagoService service, IOptions<PasarelaOptions> opciones, IPasarelaPago pasarela)
        {
            _service = service;
            _opciones = opciones.Value;
            _pasarela = pasarela;
        }

        // GET: api/pagos?reservaId=...
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] Guid? reservaId)
        {
            return Ok(await _service.ListarAsync(reservaId));
        }

        // GET: api/pagos/pasarela
        // Informa qué pasarela está activa y cómo se comporta.
        // Resulta útil durante la demostración para saber si los cobros se
        // aprobarán o se rechazarán.
        [HttpGet("pasarela")]
        public IActionResult Pasarela()
        {
            return Ok(new
            {
                proveedor = _pasarela.Nombre,
                moneda = _opciones.Moneda.ToUpperInvariant(),
                modoSimulado = _opciones.Proveedor.Equals("Simulada", StringComparison.OrdinalIgnoreCase)
                    ? _opciones.Simulada.Modo.ToString()
                    : null
            });
        }
    }
}
