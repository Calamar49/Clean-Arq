namespace Reservas.Application.Configuracion
{
    // Vencimiento de los bloqueos de habitación.
    //
    // La compensación normal de la Saga la dispara un evento: si el pago se
    // rechaza, las noches se liberan. Pero si el servicio de Pagos no responde,
    // no llega ningún evento y esas noches quedarían retenidas para siempre.
    // Este vencimiento es la red de seguridad para ese caso.
    public class VencimientoOptions
    {
        public const string Seccion = "Vencimiento";

        // Permite apagarlo durante una demostración en la que convenga
        // mantener una reserva bloqueada el tiempo que haga falta.
        public bool Activo { get; set; } = true;

        // Minutos que una reserva puede permanecer bloqueada sin resolverse
        // antes de que se libere la habitación.
        public int MinutosLimite { get; set; } = 15;

        // Cada cuánto se revisa si hay bloqueos vencidos.
        public int IntervaloSegundos { get; set; } = 60;
    }
}
