using Application.DTOs;
using Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Indica que esta clase es un controlador de API.
    // ASP.NET Core realizará automáticamente la validación
    // de los DTOs recibidos.
    [ApiController]
    // Define la ruta principal del recurso.
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        // El controlador depende del contrato del servicio,
        // no de una implementación concreta.
        private readonly IClienteService _service;

        // Recibimos el servicio mediante inyección
        // de dependencias por constructor.
        public ClientesController(IClienteService service)
        {
            _service = service;
        }

        // GET: api/clientes
        // Devuelve todos los clientes registrados.
        [HttpGet]
        public IActionResult ObtenerTodos()
        {
            var clientes = _service.ObtenerTodos();

            // HTTP 200: consulta realizada correctamente.
            return Ok(clientes);
        }

        // GET: api/clientes/1
        // Consulta un cliente por su identificador.
        [HttpGet("{id:int}")]
        public IActionResult ObtenerPorId(int id)
        {
            var cliente = _service.ObtenerPorId(id);

            // HTTP 404: el recurso solicitado no existe.
            if (cliente is null)
                return NotFound(new
                {
                    mensaje = "Cliente no encontrado."
                });

            // HTTP 200: devolvemos el cliente encontrado.
            return Ok(cliente);
        }

        // POST: api/clientes
        // Registra un cliente nuevo.
        [HttpPost]
        public IActionResult Crear(CrearClienteDTO dto)
        {
            // Delegamos el registro a Application.
            var cliente = _service.Crear(dto);

            // HTTP 201: recurso creado correctamente.
            // Incluimos la ubicación para consultarlo.
            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = cliente.Id },
                cliente);
        }

        // PUT: api/clientes/1
        // Actualiza los datos de un cliente existente.
        [HttpPut("{id:int}")]
        public IActionResult Actualizar(
            int id,
            CrearClienteDTO dto)
        {
            var actualizado = _service.Actualizar(id, dto);

            // Si el cliente no existe, devolvemos HTTP 404.
            if (!actualizado)
                return NotFound(new
                {
                    mensaje = "Cliente no encontrado."
                });

            // HTTP 204: actualización correcta,
            // sin contenido en la respuesta.
            return NoContent();
        }

        // DELETE: api/clientes/1
        // Elimina un cliente por su identificador.
        [HttpDelete("{id:int}")]
        public IActionResult Eliminar(int id)
        {
            var eliminado = _service.Eliminar(id);

            if (!eliminado)
                return NotFound(new
                {
                    mensaje = "Cliente no encontrado."
                });

            // HTTP 204: eliminación realizada.
            return NoContent();
        }

        // PATCH: api/clientes/1/desactivar
        // Ejercicio Propuesto 01: desactiva un cliente sin eliminar su registro.
        [HttpPatch("{id:int}/desactivar")]
        public IActionResult Desactivar(int id)
        {
            var desactivado = _service.Desactivar(id);

            if (!desactivado)
                return NotFound(new
                {
                    mensaje = "Cliente no encontrado."
                });

            // HTTP 204: el cliente quedó marcado como inactivo.
            return NoContent();
        }

        // PATCH: api/clientes/1/activar
        // Extensión del Ejercicio Propuesto 01: revierte la desactivación.
        [HttpPatch("{id:int}/activar")]
        public IActionResult Activar(int id)
        {
            var activado = _service.Activar(id);

            if (!activado)
                return NotFound(new
                {
                    mensaje = "Cliente no encontrado."
                });

            // HTTP 204: el cliente quedó marcado como activo nuevamente.
            return NoContent();
        }

        // GET: api/clientes/buscar?nombre=Ana
        // Ejercicio Propuesto 02: busca clientes por coincidencia parcial de nombre.
        [HttpGet("buscar")]
        public IActionResult BuscarPorNombre([FromQuery] string nombre)
        {
            var clientes = _service.BuscarPorNombre(nombre);

            // HTTP 200: siempre se responde con una lista,
            // aunque esté vacía cuando no hay coincidencias.
            return Ok(clientes);
        }
    }
}
