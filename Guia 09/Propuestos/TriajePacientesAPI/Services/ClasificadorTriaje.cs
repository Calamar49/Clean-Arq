using TriajePacientesAPI.Models;

namespace TriajePacientesAPI.Services
{
    // Determina el nivel de prioridad médica a partir de los signos vitales.
    //
    // La regla se aísla aquí, fuera del controlador, por dos motivos: puede
    // probarse sin levantar la API, y si mañana cambian los umbrales solo se
    // toca este archivo.
    public static class ClasificadorTriaje
    {
        public const string Critico = "Crítico";
        public const string Urgente = "Urgente";
        public const string Normal = "Normal";

        // Umbrales del enunciado.
        private const double TemperaturaCritica = 39.0;
        private const double TemperaturaUrgenteMinima = 37.0;
        private const int PulsoCritico = 120;
        private const int PulsoUrgenteMinimo = 90;

        // Los dos signos se evalúan con O lógico, no con Y: basta con que uno
        // esté alterado para elevar la prioridad. Un paciente con 40 °C es
        // crítico aunque su pulso sea normal, y exigir ambas condiciones
        // dejaría fuera de la categoría a quien más la necesita.
        public static string Clasificar(PacienteTriaje paciente)
        {
            if (paciente.Temperatura > TemperaturaCritica ||
                paciente.FrecuenciaCardiaca > PulsoCritico)
            {
                return Critico;
            }

            var temperaturaUrgente =
                paciente.Temperatura >= TemperaturaUrgenteMinima &&
                paciente.Temperatura <= TemperaturaCritica;

            var pulsoUrgente =
                paciente.FrecuenciaCardiaca >= PulsoUrgenteMinimo &&
                paciente.FrecuenciaCardiaca <= PulsoCritico;

            if (temperaturaUrgente || pulsoUrgente)
            {
                return Urgente;
            }

            return Normal;
        }
    }
}
