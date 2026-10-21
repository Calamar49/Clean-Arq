namespace Hotel.Messaging
{
    // Configuración de la mensajería, leída desde appsettings.json.
    public class RabbitMqOptions
    {
        public const string Seccion = "RabbitMq";

        public string Host { get; set; } = "localhost";
        public int Puerto { get; set; } = 5672;
        public string Usuario { get; set; } = "guest";
        public string Clave { get; set; } = "guest";

        // Exchange de tipo topic por el que viajan todos los eventos de la Saga.
        public string Exchange { get; set; } = "hotel.events";

        // Número máximo de intentos antes de enviar el mensaje a la cola de errores.
        public int MaxIntentos { get; set; } = 3;

        // Tiempo de espera (ms) entre reintentos; lo aplica la cola de reintentos.
        public int RetrasoReintentoMs { get; set; } = 5000;

        // Mensajes que cada consumidor procesa a la vez.
        public ushort Prefetch { get; set; } = 10;
    }
}
