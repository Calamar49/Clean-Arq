using Hotel.Contracts;
using Reservas.Domain.Entities;
using Reservas.Domain.Exceptions;

namespace Hotel.UnitTests.Dominio;

// La entidad Reserva concentra las reglas de la Saga: estas pruebas verifican
// que solo admita los cambios de estado válidos y que calcule bien el importe.
public class ReservaTests
{
    private static readonly DateOnly Entrada = DateOnly.FromDateTime(DateTime.Today).AddDays(10);
    private static readonly DateOnly Salida = Entrada.AddDays(3);

    private static Reserva CrearReserva(decimal precio = 180m) =>
        new(3, "103", "Manuel Cuchuyrumi", "manuel@ejemplo.com", Entrada, Salida, precio, MetodosPago.TarjetaAprobada);

    // ---------- Creación ----------

    [Fact]
    public void AlCrearse_CalculaNochesYMontoTotal()
    {
        var reserva = CrearReserva(precio: 180m);

        Assert.Equal(3, reserva.Noches);
        Assert.Equal(540m, reserva.MontoTotal);
    }

    [Fact]
    public void AlCrearse_QuedaEnEstadoPendienteConVersionInicial()
    {
        var reserva = CrearReserva();

        Assert.Equal(EstadoReserva.Pendiente, reserva.Estado);
        Assert.Equal(1, reserva.Version);
        Assert.NotEqual(Guid.Empty, reserva.Id);
    }

    [Fact]
    public void AlCrearse_NormalizaElCorreoAMinusculas()
    {
        var reserva = new Reserva(
            3, "103", "  Ana Torres  ", "  ANA@Ejemplo.COM ", Entrada, Salida, 150m, MetodosPago.TarjetaAprobada);

        Assert.Equal("ana@ejemplo.com", reserva.HuespedEmail);
        Assert.Equal("Ana Torres", reserva.HuespedNombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AlCrearse_RechazaNombreVacio(string nombre)
    {
        Assert.Throws<ArgumentException>(() =>
            new Reserva(3, "103", nombre, "a@b.com", Entrada, Salida, 100m, MetodosPago.TarjetaAprobada));
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("")]
    public void AlCrearse_RechazaCorreoInvalido(string email)
    {
        Assert.Throws<ArgumentException>(() =>
            new Reserva(3, "103", "Ana Torres", email, Entrada, Salida, 100m, MetodosPago.TarjetaAprobada));
    }

    [Fact]
    public void AlCrearse_RechazaSalidaAnteriorOIgualALaEntrada()
    {
        Assert.Throws<ArgumentException>(() =>
            new Reserva(3, "103", "Ana Torres", "a@b.com", Entrada, Entrada, 100m, MetodosPago.TarjetaAprobada));
    }

    [Fact]
    public void AlCrearse_RechazaFechaDeEntradaEnElPasado()
    {
        var ayer = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

        Assert.Throws<ArgumentException>(() =>
            new Reserva(3, "103", "Ana Torres", "a@b.com", ayer, ayer.AddDays(2), 100m, MetodosPago.TarjetaAprobada));
    }

    [Fact]
    public void AlCrearse_RechazaEstadiasMayoresAlMaximoPermitido()
    {
        var salidaLarga = Entrada.AddDays(Reserva.MaximoNoches + 1);

        Assert.Throws<ArgumentException>(() =>
            new Reserva(3, "103", "Ana Torres", "a@b.com", Entrada, salidaLarga, 100m, MetodosPago.TarjetaAprobada));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void AlCrearse_RechazaPrecioNoPositivo(decimal precio)
    {
        Assert.Throws<ArgumentException>(() =>
            new Reserva(3, "103", "Ana Torres", "a@b.com", Entrada, Salida, precio, MetodosPago.TarjetaAprobada));
    }

    // ---------- Camino feliz de la Saga ----------

    [Fact]
    public void CaminoFeliz_PendienteBloqueadaConfirmada()
    {
        var reserva = CrearReserva();

        reserva.MarcarBloqueada();
        Assert.Equal(EstadoReserva.Bloqueada, reserva.Estado);

        reserva.Confirmar();
        Assert.Equal(EstadoReserva.Confirmada, reserva.Estado);
    }

    [Fact]
    public void CadaCambioDeEstado_IncrementaLaVersion()
    {
        var reserva = CrearReserva();
        Assert.Equal(1, reserva.Version);

        reserva.MarcarBloqueada();
        Assert.Equal(2, reserva.Version);

        reserva.Confirmar();
        Assert.Equal(3, reserva.Version);
    }

    // ---------- Transiciones inválidas ----------

    [Fact]
    public void Confirmar_FallaSiLaReservaSigueEnPendiente()
    {
        var reserva = CrearReserva();

        Assert.Throws<TransicionInvalidaException>(() => reserva.Confirmar());
    }

    [Fact]
    public void MarcarBloqueada_FallaSiYaEstaConfirmada()
    {
        var reserva = CrearReserva();
        reserva.MarcarBloqueada();
        reserva.Confirmar();

        Assert.Throws<TransicionInvalidaException>(() => reserva.MarcarBloqueada());
    }

    [Fact]
    public void MarcarBloqueada_DosVecesSeguidas_Falla()
    {
        // Es el caso de un mensaje reentregado por RabbitMQ.
        var reserva = CrearReserva();
        reserva.MarcarBloqueada();

        Assert.Throws<TransicionInvalidaException>(() => reserva.MarcarBloqueada());
    }

    [Fact]
    public void Cancelar_FallaSiLaReservaYaEstaCancelada()
    {
        var reserva = CrearReserva();
        reserva.Cancelar("Cambio de planes");

        Assert.Throws<TransicionInvalidaException>(() => reserva.Cancelar("Otra vez"));
    }

    [Fact]
    public void Cancelar_FallaSiLaReservaFueRechazada()
    {
        var reserva = CrearReserva();
        reserva.Rechazar("Sin disponibilidad");

        Assert.Throws<TransicionInvalidaException>(() => reserva.Cancelar("Cambio de planes"));
    }

    // ---------- Rechazo y cancelación ----------

    [Fact]
    public void Rechazar_GuardaElMotivoRecibido()
    {
        var reserva = CrearReserva();
        reserva.MarcarBloqueada();

        reserva.Rechazar("La tarjeta fue rechazada.");

        Assert.Equal(EstadoReserva.Rechazada, reserva.Estado);
        Assert.Equal("La tarjeta fue rechazada.", reserva.Motivo);
    }

    [Fact]
    public void Rechazar_ExigeIndicarUnMotivo()
    {
        var reserva = CrearReserva();

        Assert.Throws<ArgumentException>(() => reserva.Rechazar("   "));
    }

    [Fact]
    public void Cancelar_SinMotivo_UsaUnTextoPorDefecto()
    {
        var reserva = CrearReserva();

        reserva.Cancelar("");

        Assert.Equal(EstadoReserva.Cancelada, reserva.Estado);
        Assert.False(string.IsNullOrWhiteSpace(reserva.Motivo));
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Bloqueada")]
    [InlineData("Confirmada")]
    public void Cancelar_SePermiteDesdeLosEstadosEnCurso(string estadoInicial)
    {
        var reserva = CrearReserva();

        if (estadoInicial != "Pendiente") reserva.MarcarBloqueada();
        if (estadoInicial == "Confirmada") reserva.Confirmar();

        reserva.Cancelar("Cambio de planes");

        Assert.Equal(EstadoReserva.Cancelada, reserva.Estado);
    }

    // ---------- Compensación ----------

    [Fact]
    public void RequiereLiberarHabitacion_SoloCuandoHayNochesBloqueadas()
    {
        var pendiente = CrearReserva();
        Assert.False(pendiente.RequiereLiberarHabitacion());

        var bloqueada = CrearReserva();
        bloqueada.MarcarBloqueada();
        Assert.True(bloqueada.RequiereLiberarHabitacion());

        var confirmada = CrearReserva();
        confirmada.MarcarBloqueada();
        confirmada.Confirmar();
        Assert.True(confirmada.RequiereLiberarHabitacion());

        var rechazada = CrearReserva();
        rechazada.Rechazar("Sin disponibilidad");
        Assert.False(rechazada.RequiereLiberarHabitacion());
    }
}
