namespace Habitaciones.Application.Exceptions
{
    // Se incumple una regla de negocio o los datos recibidos no son válidos (HTTP 400).
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje)
        {
        }
    }

    // El recurso solicitado no existe (HTTP 404).
    public class NoEncontradoException : Exception
    {
        public NoEncontradoException(string mensaje) : base(mensaje)
        {
        }
    }

    // La operación entra en conflicto con el estado actual (HTTP 409).
    public class ConflictoException : Exception
    {
        public ConflictoException(string mensaje) : base(mensaje)
        {
        }
    }
}
