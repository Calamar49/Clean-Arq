namespace TriajePacientesAPI.Models
{
    // Paciente registrado en el sistema de triaje.
    //
    // NivelPrioridad no lo envía el cliente: lo calcula el servidor a partir
    // de los signos vitales, de modo que nadie pueda declararse "Crítico"
    // desde fuera.
    public class PacienteTriaje
    {
        // Identificador del paciente.
        public int Id { get; set; }

        // Nombre completo del paciente.
        public string Nombre { get; set; } = string.Empty;

        // Edad en años.
        public int Edad { get; set; }

        // Temperatura corporal en grados centígrados.
        public double Temperatura { get; set; }

        // Latidos por minuto.
        public int FrecuenciaCardiaca { get; set; }

        // Nivel de prioridad médica: Crítico, Urgente o Normal.
        // Se calcula automáticamente y se ignora lo que llegue en la petición.
        public string NivelPrioridad { get; set; } = string.Empty;
    }
}
