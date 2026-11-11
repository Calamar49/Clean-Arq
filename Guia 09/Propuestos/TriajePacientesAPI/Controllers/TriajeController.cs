using Microsoft.AspNetCore.Mvc;
using TriajePacientesAPI.Models;
using TriajePacientesAPI.Services;

namespace TriajePacientesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TriajeController : ControllerBase
    {
        // Lista en memoria: la guía no incorpora base de datos todavía.
        private static readonly List<PacienteTriaje> pacientes = new()
        {
            new PacienteTriaje
            {
                Id = 1,
                Nombre = "Rosa Quispe Huamán",
                Edad = 34,
                Temperatura = 36.5,
                FrecuenciaCardiaca = 78,
                NivelPrioridad = ClasificadorTriaje.Normal
            },
            new PacienteTriaje
            {
                Id = 2,
                Nombre = "Julio Mamani Ccopa",
                Edad = 61,
                Temperatura = 38.2,
                FrecuenciaCardiaca = 96,
                NivelPrioridad = ClasificadorTriaje.Urgente
            },
            new PacienteTriaje
            {
                Id = 3,
                Nombre = "Elena Ttito Ccahuana",
                Edad = 7,
                Temperatura = 39.8,
                FrecuenciaCardiaca = 134,
                NivelPrioridad = ClasificadorTriaje.Critico
            }
        };

        // Lista todos los pacientes registrados en el triaje.
        [HttpGet]
        public ActionResult<IEnumerable<PacienteTriaje>> Get()
        {
            // Los críticos primero: es el orden en que los atendería el área.
            var ordenados = pacientes
                .OrderBy(p => p.NivelPrioridad switch
                {
                    ClasificadorTriaje.Critico => 0,
                    ClasificadorTriaje.Urgente => 1,
                    _ => 2
                })
                .ThenBy(p => p.Id);

            return Ok(ordenados);
        }

        // Registra un paciente y determina su prioridad automáticamente.
        [HttpPost]
        public ActionResult<PacienteTriaje> Post([FromBody] PacienteTriaje nuevoPaciente)
        {
            if (string.IsNullOrWhiteSpace(nuevoPaciente.Nombre))
            {
                return BadRequest(new
                {
                    mensaje = "El nombre del paciente es obligatorio."
                });
            }

            if (nuevoPaciente.Edad <= 0 || nuevoPaciente.Edad > 120)
            {
                return BadRequest(new
                {
                    mensaje = "La edad debe estar entre 1 y 120 años."
                });
            }

            if (nuevoPaciente.Temperatura < 30 || nuevoPaciente.Temperatura > 45)
            {
                return BadRequest(new
                {
                    mensaje = "La temperatura debe estar entre 30 y 45 grados."
                });
            }

            if (nuevoPaciente.FrecuenciaCardiaca <= 0 || nuevoPaciente.FrecuenciaCardiaca > 300)
            {
                return BadRequest(new
                {
                    mensaje = "La frecuencia cardíaca debe estar entre 1 y 300 latidos por minuto."
                });
            }

            nuevoPaciente.Id = pacientes.Count == 0 ? 1 : pacientes.Max(p => p.Id) + 1;

            // Se ignora cualquier nivel que venga en la petición: lo decide
            // el servidor a partir de los signos vitales.
            nuevoPaciente.NivelPrioridad = ClasificadorTriaje.Clasificar(nuevoPaciente);

            pacientes.Add(nuevoPaciente);

            return CreatedAtAction(nameof(Get), new { id = nuevoPaciente.Id }, nuevoPaciente);
        }
    }
}
