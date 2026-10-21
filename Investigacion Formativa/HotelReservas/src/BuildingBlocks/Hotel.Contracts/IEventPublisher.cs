namespace Hotel.Contracts
{
    // Contrato para publicar eventos de la Saga.
    //
    // Vive junto a los contratos (sin dependencias externas) para que las capas
    // de aplicación puedan publicar eventos sin conocer RabbitMQ. La
    // implementación concreta se registra desde la capa de infraestructura.
    public interface IEventPublisher
    {
        Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : IEvento;
    }
}
