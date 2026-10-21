namespace Hotel.Contracts
{
    // Medios de pago admitidos en el entorno de demostración.
    //
    // El sistema nunca recibe ni almacena datos reales de tarjeta: el cliente
    // envía únicamente un identificador del medio elegido, y cada pasarela lo
    // traduce a su propio método de prueba. Es el mismo principio que siguen
    // las pasarelas reales, donde el servidor solo maneja un token.
    public static class MetodosPago
    {
        // Tarjeta de prueba cuyo cobro se aprueba.
        public const string TarjetaAprobada = "tarjeta-aprobada";

        // Tarjeta de prueba cuyo cobro es rechazado por la entidad emisora.
        // Permite demostrar la compensación de la Saga.
        public const string TarjetaRechazada = "tarjeta-rechazada";

        public static readonly string[] Admitidos =
        [
            TarjetaAprobada,
            TarjetaRechazada
        ];

        public static bool EsValido(string? metodo) =>
            !string.IsNullOrWhiteSpace(metodo) && Admitidos.Contains(metodo);

        public static string Descripcion(string metodo) => metodo switch
        {
            TarjetaRechazada => "Visa de prueba ****0002 (rechazada)",
            _ => "Visa de prueba ****4242 (aprobada)"
        };
    }
}
