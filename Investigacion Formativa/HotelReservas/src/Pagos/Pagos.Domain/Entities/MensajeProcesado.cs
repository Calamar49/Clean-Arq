namespace Pagos.Domain.Entities
{
    // Deja constancia de que un mensaje ya fue atendido.
    // Si RabbitMQ lo reentrega, el consumidor lo detecta y no vuelve a cobrar.
    public class MensajeProcesado
    {
        public Guid MessageId { get; private set; }
        public string RoutingKey { get; private set; } = string.Empty;
        public DateTime FechaProceso { get; private set; }

        private MensajeProcesado()
        {
        }

        public MensajeProcesado(Guid messageId, string routingKey)
        {
            MessageId = messageId;
            RoutingKey = routingKey;
            FechaProceso = DateTime.Now;
        }
    }
}
