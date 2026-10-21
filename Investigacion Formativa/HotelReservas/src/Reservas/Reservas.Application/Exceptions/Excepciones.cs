namespace Reservas.Application.Exceptions
{
    // Datos inválidos o regla de negocio incumplida (HTTP 400).
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

    // Conflicto con el estado actual, incluida la concurrencia optimista (HTTP 409).
    public class ConflictoException : Exception
    {
        public ConflictoException(string mensaje) : base(mensaje)
        {
        }
    }

    // Un servicio externo no respondió correctamente (HTTP 503).
    public class ServicioNoDisponibleException : Exception
    {
        public ServicioNoDisponibleException(string mensaje) : base(mensaje)
        {
        }
    }
}
