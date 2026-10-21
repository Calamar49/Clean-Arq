namespace Hotel.Contracts
{
    // Claves de enrutamiento de la Saga. Centralizarlas evita errores de escritura
    // entre el servicio que publica y el que consume.
    public static class RoutingKeys
    {
        public const string ReservaSolicitada = "reserva.solicitada";
        public const string HabitacionBloqueada = "habitacion.bloqueada";
        public const string HabitacionNoDisponible = "habitacion.nodisponible";
        public const string ReservaBloqueada = "reserva.bloqueada";
        public const string PagoAprobado = "pago.aprobado";
        public const string PagoRechazado = "pago.rechazado";
        public const string ReservaConfirmada = "reserva.confirmada";
        public const string ReservaRechazada = "reserva.rechazada";
        public const string ReservaCancelada = "reserva.cancelada";
    }

    // --------------------------------------------------------------------
    // Paso 1: el huésped solicita una reserva.
    // Publica Reservas, consume Habitaciones.
    // --------------------------------------------------------------------
    public record ReservaSolicitada(
        Guid ReservaId,
        int HabitacionId,
        string HabitacionNumero,
        DateOnly FechaEntrada,
        DateOnly FechaSalida,
        decimal MontoTotal,
        string HuespedNombre,
        string HuespedEmail,
        string MetodoPago) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.ReservaSolicitada;
    }

    // --------------------------------------------------------------------
    // Paso 2a: las noches quedaron bloqueadas.
    // Publica Habitaciones, consume Reservas.
    // --------------------------------------------------------------------
    public record HabitacionBloqueada(
        Guid ReservaId,
        int HabitacionId) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.HabitacionBloqueada;
    }

    // --------------------------------------------------------------------
    // Paso 2b: alguna noche ya estaba ocupada.
    // Publica Habitaciones, consume Reservas.
    // --------------------------------------------------------------------
    public record HabitacionNoDisponible(
        Guid ReservaId,
        int HabitacionId,
        string Motivo) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.HabitacionNoDisponible;
    }

    // --------------------------------------------------------------------
    // Paso 3: la reserva pasó a Bloqueada y puede cobrarse.
    // Publica Reservas, consume Pagos.
    // Este paso intermedio evita que el pago se procese antes de que la
    // reserva haya registrado el bloqueo.
    // --------------------------------------------------------------------
    public record ReservaBloqueada(
        Guid ReservaId,
        decimal MontoTotal,
        string HuespedEmail,
        string MetodoPago) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.ReservaBloqueada;
    }

    // --------------------------------------------------------------------
    // Paso 4a: el pago fue aprobado.
    // Publica Pagos, consume Reservas.
    // --------------------------------------------------------------------
    public record PagoAprobado(
        Guid ReservaId,
        string Referencia,
        decimal Monto) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.PagoAprobado;
    }

    // --------------------------------------------------------------------
    // Paso 4b: el pago fue rechazado. Dispara la compensación.
    // Publica Pagos, consume Reservas.
    // --------------------------------------------------------------------
    public record PagoRechazado(
        Guid ReservaId,
        string Motivo) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.PagoRechazado;
    }

    // --------------------------------------------------------------------
    // Fin feliz de la Saga.
    // Publica Reservas, consume Notificaciones.
    // --------------------------------------------------------------------
    public record ReservaConfirmada(
        Guid ReservaId,
        string HabitacionNumero,
        string HuespedNombre,
        string HuespedEmail,
        DateOnly FechaEntrada,
        DateOnly FechaSalida,
        decimal MontoTotal) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.ReservaConfirmada;
    }

    // --------------------------------------------------------------------
    // Fin alternativo: la reserva no prosperó.
    // Publica Reservas; consumen Habitaciones (compensación: liberar las
    // noches) y Notificaciones (avisar al huésped).
    // --------------------------------------------------------------------
    public record ReservaRechazada(
        Guid ReservaId,
        string HuespedNombre,
        string HuespedEmail,
        string Motivo) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.ReservaRechazada;
    }

    // --------------------------------------------------------------------
    // El huésped canceló la reserva.
    // Publica Reservas; consumen Habitaciones y Notificaciones.
    // --------------------------------------------------------------------
    public record ReservaCancelada(
        Guid ReservaId,
        string HuespedNombre,
        string HuespedEmail,
        string Motivo) : IEvento
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime FechaEvento { get; init; } = DateTime.Now;
        public string RoutingKey => RoutingKeys.ReservaCancelada;
    }
}
