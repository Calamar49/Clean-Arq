namespace Hotel.Contracts
{
    // Contrato común de todos los eventos de la Saga.
    public interface IEvento
    {
        // Identificador único del mensaje. Permite que los consumidores
        // detecten reentregas y no procesen dos veces el mismo evento.
        Guid MessageId { get; }

        // Clave con la que el exchange enruta el mensaje hacia las colas.
        string RoutingKey { get; }

        DateTime FechaEvento { get; }
    }
}
