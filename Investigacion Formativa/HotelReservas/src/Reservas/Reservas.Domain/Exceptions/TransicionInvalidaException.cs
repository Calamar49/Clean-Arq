namespace Reservas.Domain.Exceptions
{
    // Se intentó un cambio de estado que la Saga no permite,
    // por ejemplo confirmar una reserva ya cancelada.
    public class TransicionInvalidaException : Exception
    {
        public TransicionInvalidaException(string mensaje) : base(mensaje)
        {
        }
    }
}
