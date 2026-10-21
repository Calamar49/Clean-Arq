namespace Notificaciones.Domain.Entities
{
    // Evita que un mensaje reentregado genere un aviso duplicado.
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
