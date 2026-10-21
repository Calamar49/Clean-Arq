using ClienteHotelMVC.Models;
using ClienteHotelMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClienteHotelMVC.Controllers
{
    public class ReservasController : Controller
    {
        private readonly HabitacionesApiClient _habitaciones;
        private readonly ReservasApiClient _reservas;

        public ReservasController(
            HabitacionesApiClient habitaciones, ReservasApiClient reservas)
        {
            _habitaciones = habitaciones;
            _reservas = reservas;
        }

        // Buscador de disponibilidad (página inicial).
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            return View(new BuscarDisponibilidadVM
            {
                FechaEntrada = hoy.AddDays(1),
                FechaSalida = hoy.AddDays(3),
                Tipos = await ObtenerTiposAsync()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(BuscarDisponibilidadVM modelo)
        {
            modelo.Tipos = await ObtenerTiposAsync();

            if (!ModelState.IsValid)
                return View(modelo);

            if (modelo.FechaSalida <= modelo.FechaEntrada)
            {
                ModelState.AddModelError(nameof(modelo.FechaSalida),
                    "La fecha de salida debe ser posterior a la de llegada.");
                return View(modelo);
            }

            if (modelo.FechaEntrada < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(nameof(modelo.FechaEntrada),
                    "La fecha de llegada no puede estar en el pasado.");
                return View(modelo);
            }

            try
            {
                modelo.Resultados = await _habitaciones.BuscarDisponiblesAsync(
                    modelo.FechaEntrada!.Value, modelo.FechaSalida!.Value, modelo.Huespedes);

                modelo.Buscado = true;
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View(modelo);
        }

        // Agrupa el catálogo por tipo de habitación para la página inicial.
        private async Task<List<TipoHabitacionVM>> ObtenerTiposAsync()
        {
            var orden = new[] { "Simple", "Doble", "Suite" };

            var catalogo = await _habitaciones.ListarCatalogoAsync();

            return catalogo
                .Where(h => h.Activa)
                .GroupBy(h => h.Tipo)
                .Select(g => new TipoHabitacionVM
                {
                    Tipo = g.Key,
                    CapacidadMaxima = g.Max(h => h.Capacidad),
                    PrecioDesde = g.Min(h => h.PrecioPorNoche),
                    Cantidad = g.Count()
                })
                .OrderBy(t => Array.IndexOf(orden, t.Tipo))
                .ToList();
        }

        // Formulario con los datos del huésped.
        [HttpGet]
        public async Task<IActionResult> Reservar(
            int habitacionId, DateOnly entrada, DateOnly salida)
        {
            try
            {
                var habitacion = await _habitaciones.ObtenerAsync(habitacionId);

                if (habitacion is null)
                    return NotFound();

                return View(new CrearReservaVM
                {
                    HabitacionId = habitacion.Id,
                    HabitacionNumero = habitacion.Numero,
                    Tipo = habitacion.Tipo,
                    PrecioPorNoche = habitacion.PrecioPorNoche,
                    FechaEntrada = entrada,
                    FechaSalida = salida
                });
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Tras validar los datos del huésped se avanza al paso de pago.
        [HttpPost]
        public IActionResult Reservar(CrearReservaVM modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            return View("Pago", new PagoVM
            {
                HabitacionId = modelo.HabitacionId,
                HabitacionNumero = modelo.HabitacionNumero,
                Tipo = modelo.Tipo,
                PrecioPorNoche = modelo.PrecioPorNoche,
                FechaEntrada = modelo.FechaEntrada,
                FechaSalida = modelo.FechaSalida,
                HuespedNombre = modelo.HuespedNombre,
                HuespedEmail = modelo.HuespedEmail
            });
        }

        // Paso de pago: solo aquí se crea la reserva y se inicia la Saga.
        [HttpPost]
        public async Task<IActionResult> Pagar(PagoVM modelo)
        {
            if (!ModelState.IsValid)
                return View("Pago", modelo);

            try
            {
                var reserva = await _reservas.CrearAsync(modelo);
                return RedirectToAction(nameof(Seguimiento), new { id = reserva.Id });
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Pago", modelo);
            }
        }

        // Seguimiento de la reserva: muestra el avance de la Saga.
        [HttpGet]
        public async Task<IActionResult> Seguimiento(Guid id)
        {
            try
            {
                var reserva = await _reservas.ObtenerVistaAsync(id);

                // La proyección puede tardar unos milisegundos (consistencia
                // eventual): se muestra como "en proceso" y la página se
                // actualizará sola en cuanto aparezca.
                reserva ??= new ReservaVM { Id = id, Estado = "Pendiente" };

                return View(reserva);
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Estado actual en formato JSON, para que la página de seguimiento
        // se refresque sin recargarse por completo.
        [HttpGet]
        public async Task<IActionResult> Estado(Guid id)
        {
            var reserva = await _reservas.ObtenerVistaAsync(id);

            if (reserva is null)
                return Json(new { estado = "Pendiente", enProceso = true });

            return Json(new
            {
                estado = reserva.Estado,
                descripcion = reserva.DescripcionEstado,
                clase = reserva.ClaseEstado,
                motivo = reserva.Motivo,
                enProceso = reserva.EnProceso
            });
        }

        // Consulta de reservas por correo (lado de lectura de CQRS).
        [HttpGet]
        // Acepta el correo por la dirección para que al volver de una
        // cancelación se vuelva a mostrar el listado del huésped.
        public async Task<IActionResult> Mias(string? email)
        {
            var modelo = new MisReservasVM { Email = email ?? string.Empty };

            if (string.IsNullOrWhiteSpace(email))
                return View(modelo);

            try
            {
                modelo.Reservas = await _reservas.ConsultarPorEmailAsync(email);
                modelo.Buscado = true;
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Mias(MisReservasVM modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            try
            {
                modelo.Reservas = await _reservas.ConsultarPorEmailAsync(modelo.Email);
                modelo.Buscado = true;
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View(modelo);
        }

        // Página mostrada ante un error no controlado.
        [HttpGet]
        public IActionResult Error() => View();

        [HttpPost]
        public async Task<IActionResult> Cancelar(Guid id, string? motivo, string? email)
        {
            try
            {
                await _reservas.CancelarAsync(id, motivo ?? "Cancelada por el huésped.");
                TempData["Mensaje"] = "La reserva fue cancelada.";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return string.IsNullOrWhiteSpace(email)
                ? RedirectToAction(nameof(Seguimiento), new { id })
                : RedirectToAction(nameof(Mias), new { email });
        }
    }
}
