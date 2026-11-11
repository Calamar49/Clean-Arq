using FarmaciaPedidosAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaPedidosAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        // Lista en memoria que simula pedidos de farmacia.
        private static readonly List<PedidoFarmacia> pedidos = new()
        {
            new PedidoFarmacia
            {
                Id = 1,
                Medicamento = "Paracetamol 500 mg",
                AreaSolicitante = "Emergencia",
                Cantidad = 100,
                Fecha = DateTime.Now.AddDays(-1),
                Estado = "Pendiente"
            },
            new PedidoFarmacia
            {
                Id = 2,
                Medicamento = "Suero fisiológico",
                AreaSolicitante = "Hospitalización",
                Cantidad = 80,
                Fecha = DateTime.Now,
                Estado = "Atendido"
            }
        };

        // Lista todos los pedidos.
        [HttpGet]
        public ActionResult<IEnumerable<PedidoFarmacia>> Get()
        {
            return Ok(pedidos);
        }

        // Busca un pedido por Id.
        [HttpGet("{id}")]
        public ActionResult<PedidoFarmacia> GetById(int id)
        {
            var pedido = pedidos.FirstOrDefault(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new
                {
                    mensaje = "Pedido no encontrado."
                });
            }

            return Ok(pedido);
        }

        // Registra un nuevo pedido.
        [HttpPost]
        public ActionResult<PedidoFarmacia> Post([FromBody] PedidoFarmacia nuevoPedido)
        {
            // Validación básica de negocio.
            if (string.IsNullOrWhiteSpace(nuevoPedido.Medicamento))
            {
                return BadRequest(new
                {
                    mensaje = "El nombre del medicamento es obligatorio."
                });
            }

            if (nuevoPedido.Cantidad <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "La cantidad debe ser mayor que cero."
                });
            }

            // Generación simple de Id.
            nuevoPedido.Id = pedidos.Count == 0 ? 1 : pedidos.Max(p => p.Id) + 1;

            // Si no se envía fecha, se asigna la actual.
            if (nuevoPedido.Fecha == default)
            {
                nuevoPedido.Fecha = DateTime.Now;
            }

            // Si no se envía estado, se define por defecto.
            if (string.IsNullOrWhiteSpace(nuevoPedido.Estado))
            {
                nuevoPedido.Estado = "Pendiente";
            }

            pedidos.Add(nuevoPedido);

            return CreatedAtAction(nameof(GetById), new { id = nuevoPedido.Id }, nuevoPedido);
        }
    }
}
