namespace Pagos.Application.Configuracion
{
    // Comportamiento de la pasarela simulada durante la demostración.
    public enum ModoSimulado
    {
        // Todos los cobros se aprueban (camino feliz).
        AprobarSiempre,

        // Todos los cobros se rechazan: permite demostrar la compensación.
        RechazarSiempre,

        // Aprueba o rechaza según la tasa de aprobación configurada.
        Aleatorio
    }

    public class PasarelaOptions
    {
        public const string Seccion = "Pasarela";

        // Implementación a utilizar: "Simulada" o "Stripe".
        public string Proveedor { get; set; } = "Simulada";

        // Moneda de los cobros (la cuenta de pruebas de Stripe determina cuáles admite).
        public string Moneda { get; set; } = "mxn";

        public SimuladaOptions Simulada { get; set; } = new();

        public StripeOptions Stripe { get; set; } = new();
    }

    public class SimuladaOptions
    {
        public ModoSimulado Modo { get; set; } = ModoSimulado.AprobarSiempre;

        // Probabilidad de aprobación cuando el modo es Aleatorio.
        public double TasaAprobacion { get; set; } = 0.8;

        // Retraso artificial para que el cobro se parezca a una llamada real.
        public int RetrasoMs { get; set; } = 400;
    }

    public class StripeOptions
    {
        // La clave secreta NO se guarda en el repositorio: se lee del gestor
        // de secretos de .NET o de una variable de entorno.
        public string ClaveSecreta { get; set; } = string.Empty;

        // Método de pago de prueba de Stripe.
        //   pm_card_visa                 -> el cobro se aprueba
        //   pm_card_visa_chargeDeclined  -> el cobro se rechaza
        public string MetodoPagoPrueba { get; set; } = "pm_card_visa";
    }
}
