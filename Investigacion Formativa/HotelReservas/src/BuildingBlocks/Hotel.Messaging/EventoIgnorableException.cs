namespace Hotel.Messaging
{
    // El consumidor no puede procesar el evento y reintentarlo tampoco serviría:
    // por ejemplo, un mensaje reentregado cuya reserva ya cambió de estado.
    // El consumidor confirma el mensaje y lo descarta sin enviarlo a la cola de errores.
    public class EventoIgnorableException : Exception
    {
        public EventoIgnorableException(string mensaje) : base(mensaje)
        {
        }
    }
}
