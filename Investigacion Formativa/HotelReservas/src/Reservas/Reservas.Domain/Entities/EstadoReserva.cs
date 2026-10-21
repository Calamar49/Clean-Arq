namespace Reservas.Domain.Entities
{
    // Estados por los que pasa una reserva dentro de la Saga.
    public enum EstadoReserva
    {
        // Solicitud recibida; aún no se bloquearon las noches.
        Pendiente,

        // El servicio de Habitaciones confirmó el bloqueo de las noches.
        Bloqueada,

        // El pago fue aprobado: la reserva quedó en firme.
        Confirmada,

        // No había disponibilidad o el pago falló (con compensación).
        Rechazada,

        // El huésped canceló una reserva previamente confirmada.
        Cancelada
    }
}
