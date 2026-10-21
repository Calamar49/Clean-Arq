using RabbitMQ.Client;

namespace Hotel.Messaging
{
    // Declara el exchange y las colas de cada servicio.
    //
    // Esquema por servicio:
    //   hotel.<servicio>         cola principal, suscrita a sus claves de enrutamiento
    //   hotel.<servicio>.retry   cola de espera: retiene el mensaje y lo devuelve
    //                            a la cola principal cuando vence el TTL
    //   hotel.<servicio>.dlq     cola de errores: mensajes agotados tras N intentos
    public static class Topologia
    {
        public static string ColaPrincipal(string servicio) => $"hotel.{servicio}";

        public static string ColaReintento(string servicio) => $"hotel.{servicio}.retry";

        public static string ColaErrores(string servicio) => $"hotel.{servicio}.dlq";

        public static async Task DeclararAsync(
            IChannel canal,
            RabbitMqOptions opciones,
            string servicio,
            IEnumerable<string> routingKeys,
            CancellationToken ct = default)
        {
            // Exchange de tipo topic: permite que un mismo evento llegue a
            // varias colas según su clave de enrutamiento.
            await canal.ExchangeDeclareAsync(
                exchange: opciones.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            var principal = ColaPrincipal(servicio);
            var reintento = ColaReintento(servicio);
            var errores = ColaErrores(servicio);

            await canal.QueueDeclareAsync(
                queue: principal, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: ct);

            // Al vencer el TTL, la cola de reintento devuelve el mensaje
            // a la cola principal usando el exchange por defecto.
            await canal.QueueDeclareAsync(
                queue: reintento, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = opciones.RetrasoReintentoMs,
                    ["x-dead-letter-exchange"] = string.Empty,
                    ["x-dead-letter-routing-key"] = principal
                },
                cancellationToken: ct);

            await canal.QueueDeclareAsync(
                queue: errores, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: ct);

            foreach (var clave in routingKeys)
            {
                await canal.QueueBindAsync(
                    queue: principal,
                    exchange: opciones.Exchange,
                    routingKey: clave,
                    cancellationToken: ct);
            }
        }
    }
}
