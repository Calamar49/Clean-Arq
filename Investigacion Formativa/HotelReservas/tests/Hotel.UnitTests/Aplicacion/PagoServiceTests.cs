using Hotel.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Pagos.Application.Configuracion;
using Pagos.Application.Interfaces;
using Pagos.Application.Services;
using Pagos.Domain.Entities;

namespace Hotel.UnitTests.Aplicacion;

// El cobro es la operación más delicada de la Saga: no debe repetirse nunca
// aunque RabbitMQ reentregue el mensaje.
public class PagoServiceTests
{
    private readonly IPagoRepository _repositorio = Substitute.For<IPagoRepository>();
    private readonly IPasarelaPago _pasarela = Substitute.For<IPasarelaPago>();
    private readonly IEventPublisher _eventos = Substitute.For<IEventPublisher>();
    private readonly PagoService _servicio;

    private readonly Guid _messageId = Guid.NewGuid();
    private readonly Guid _reservaId = Guid.NewGuid();

    public PagoServiceTests()
    {
        var opciones = Options.Create(new PasarelaOptions { Moneda = "mxn" });
        _pasarela.Nombre.Returns("Simulada");

        _servicio = new PagoService(
            _repositorio, _pasarela, _eventos, NullLogger<PagoService>.Instance, opciones);
    }

    private void ConfigurarCobro(bool aprobado)
    {
        _pasarela.CobrarAsync(
                Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(aprobado
                ? ResultadoCobro.Exito("REF-123")
                : ResultadoCobro.Fallo("La tarjeta fue rechazada."));
    }

    private Task Procesar(string metodoPago = MetodosPago.TarjetaAprobada) =>
        _servicio.ProcesarCobroAsync(
            _messageId, _reservaId, 540m, "a@b.com", metodoPago, CancellationToken.None);

    // ---------- Camino feliz ----------

    [Fact]
    public async Task CobroAprobado_RegistraElPagoYPublicaElEvento()
    {
        ConfigurarCobro(aprobado: true);

        await Procesar();

        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Pago>(p => p.Estado == EstadoPago.Aprobado && p.Referencia == "REF-123"));

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<PagoAprobado>(e => e.ReservaId == _reservaId && e.Monto == 540m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CobroRechazado_PublicaElEventoQueDisparaLaCompensacion()
    {
        ConfigurarCobro(aprobado: false);

        await Procesar();

        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Pago>(p => p.Estado == EstadoPago.Rechazado));

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<PagoRechazado>(e => e.ReservaId == _reservaId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cobro_UsaLaMonedaConfigurada()
    {
        ConfigurarCobro(aprobado: true);

        await Procesar();

        await _pasarela.Received(1).CobrarAsync(
            540m, "mxn", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cobro_TrasladaALaPasarelaElMedioElegidoPorElHuesped()
    {
        ConfigurarCobro(aprobado: true);

        await Procesar(MetodosPago.TarjetaRechazada);

        await _pasarela.Received(1).CobrarAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
            MetodosPago.TarjetaRechazada, Arg.Any<CancellationToken>());
    }

    // ---------- Idempotencia ----------

    [Fact]
    public async Task MensajeYaProcesado_NoVuelveACobrar()
    {
        _repositorio.YaProcesadoAsync(_messageId).Returns(true);

        await Procesar();

        await _pasarela.DidNotReceive().CobrarAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<Pago>());
    }

    [Fact]
    public async Task MensajeYaProcesado_NoPublicaNingunEvento()
    {
        _repositorio.YaProcesadoAsync(_messageId).Returns(true);

        await Procesar();

        await _eventos.DidNotReceive().PublicarAsync(
            Arg.Any<PagoAprobado>(), Arg.Any<CancellationToken>());
        await _eventos.DidNotReceive().PublicarAsync(
            Arg.Any<PagoRechazado>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservaConPagoPrevio_NoCobraDosVeces()
    {
        // Segunda salvaguarda: aunque llegue un mensaje distinto, la reserva
        // ya tiene un pago registrado.
        _repositorio.ObtenerPorReservaAsync(_reservaId)
            .Returns(Pago.Aprobado(_reservaId, 540m, "mxn", "Simulada", "REF-ANTERIOR"));

        await Procesar();

        await _pasarela.DidNotReceive().CobrarAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservaConPagoPrevio_DejaConstanciaDelMensajeProcesado()
    {
        _repositorio.ObtenerPorReservaAsync(_reservaId)
            .Returns(Pago.Aprobado(_reservaId, 540m, "mxn", "Simulada", "REF-ANTERIOR"));

        await Procesar();

        await _repositorio.Received(1).RegistrarProcesadoAsync(_messageId, Arg.Any<string>());
    }

    [Fact]
    public async Task CobroNuevo_DejaConstanciaDelMensajeProcesado()
    {
        ConfigurarCobro(aprobado: true);

        await Procesar();

        await _repositorio.Received(1).RegistrarProcesadoAsync(_messageId, Arg.Any<string>());
    }
}
