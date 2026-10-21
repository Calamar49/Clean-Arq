using Notificaciones.Domain.Entities;
using Pagos.Domain.Entities;

namespace Hotel.UnitTests.Dominio;

public class PagoTests
{
    [Fact]
    public void Aprobado_RegistraLaReferenciaDeLaPasarela()
    {
        var reservaId = Guid.NewGuid();

        var pago = Pago.Aprobado(reservaId, 540m, "MXN", "Stripe", "pi_123456");

        Assert.Equal(EstadoPago.Aprobado, pago.Estado);
        Assert.Equal("pi_123456", pago.Referencia);
        Assert.Equal("Stripe", pago.Pasarela);
        Assert.Equal(reservaId, pago.ReservaId);
        Assert.Null(pago.Motivo);
    }

    [Fact]
    public void Rechazado_RegistraElMotivoYNoDejaReferencia()
    {
        var pago = Pago.Rechazado(
            Guid.NewGuid(), 540m, "MXN", "Simulada", "La tarjeta fue rechazada.");

        Assert.Equal(EstadoPago.Rechazado, pago.Estado);
        Assert.Equal("La tarjeta fue rechazada.", pago.Motivo);
        Assert.Null(pago.Referencia);
    }

    [Fact]
    public void AlCrearse_NormalizaLaMonedaAMinusculas()
    {
        var pago = Pago.Aprobado(Guid.NewGuid(), 100m, "MXN", "Simulada", "REF");

        Assert.Equal("mxn", pago.Moneda);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AlCrearse_RechazaMontoNoPositivo(decimal monto)
    {
        Assert.Throws<ArgumentException>(() =>
            Pago.Aprobado(Guid.NewGuid(), monto, "MXN", "Simulada", "REF"));
    }

    [Fact]
    public void AlCrearse_RechazaReservaVacia()
    {
        Assert.Throws<ArgumentException>(() =>
            Pago.Aprobado(Guid.Empty, 100m, "MXN", "Simulada", "REF"));
    }
}

public class NotificacionTests
{
    [Fact]
    public void AlCrearse_NormalizaElDestinatario()
    {
        var notificacion = new Notificacion(
            Guid.NewGuid(), "  ANA@Ejemplo.COM  ",
            TipoNotificacion.ReservaConfirmada, "Asunto", "Cuerpo");

        Assert.Equal("ana@ejemplo.com", notificacion.Destinatario);
    }

    [Fact]
    public void AlCrearse_RechazaDestinatarioVacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notificacion(Guid.NewGuid(), "   ",
                TipoNotificacion.ReservaConfirmada, "Asunto", "Cuerpo"));
    }

    [Fact]
    public void AlCrearse_RechazaReservaVacia()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notificacion(Guid.Empty, "a@b.com",
                TipoNotificacion.ReservaConfirmada, "Asunto", "Cuerpo"));
    }
}
