using Hotel.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Notificaciones.Application.Interfaces;
using Notificaciones.Application.Services;
using Notificaciones.Domain.Entities;
using NSubstitute;

namespace Hotel.UnitTests.Aplicacion;

public class NotificacionServiceTests
{
    private readonly INotificacionRepository _repositorio =
        Substitute.For<INotificacionRepository>();

    private readonly NotificacionService _servicio;

    private readonly Guid _messageId = Guid.NewGuid();
    private readonly Guid _reservaId = Guid.NewGuid();

    public NotificacionServiceTests()
    {
        _servicio = new NotificacionService(
            _repositorio, NullLogger<NotificacionService>.Instance);
    }

    private Task Registrar(string routingKey, string? detalle = "Detalle de la reserva") =>
        _servicio.RegistrarAsync(
            _messageId, routingKey, _reservaId,
            "Ana Torres", "ana@ejemplo.com", detalle, CancellationToken.None);

    [Fact]
    public async Task ReservaConfirmada_GeneraUnAvisoDeConfirmacion()
    {
        await Registrar(RoutingKeys.ReservaConfirmada);

        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Notificacion>(n =>
                n.Tipo == TipoNotificacion.ReservaConfirmada &&
                n.Destinatario == "ana@ejemplo.com" &&
                n.ReservaId == _reservaId));
    }

    [Fact]
    public async Task ReservaRechazada_IncluyeElMotivoEnElCuerpo()
    {
        await Registrar(RoutingKeys.ReservaRechazada, "La tarjeta fue rechazada.");

        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Notificacion>(n =>
                n.Tipo == TipoNotificacion.ReservaRechazada &&
                n.Cuerpo.Contains("La tarjeta fue rechazada.")));
    }

    [Fact]
    public async Task ReservaCancelada_GeneraUnAvisoDeCancelacion()
    {
        await Registrar(RoutingKeys.ReservaCancelada, "Cambio de planes");

        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Notificacion>(n => n.Tipo == TipoNotificacion.ReservaCancelada));
    }

    [Fact]
    public async Task ElAviso_MencionaAlHuespedPorSuNombre()
    {
        await Registrar(RoutingKeys.ReservaConfirmada);

        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Notificacion>(n => n.Cuerpo.Contains("Ana Torres")));
    }

    [Fact]
    public async Task MensajeYaProcesado_NoGeneraUnAvisoDuplicado()
    {
        _repositorio.YaProcesadoAsync(_messageId).Returns(true);

        await Registrar(RoutingKeys.ReservaConfirmada);

        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<Notificacion>());
    }

    [Fact]
    public async Task AvisoNuevo_DejaConstanciaDelMensajeProcesado()
    {
        await Registrar(RoutingKeys.ReservaConfirmada);

        await _repositorio.Received(1).RegistrarProcesadoAsync(
            _messageId, RoutingKeys.ReservaConfirmada);
    }

    [Fact]
    public async Task Listar_DevuelveLosAvisosDelDestinatario()
    {
        _repositorio.ListarAsync("ana@ejemplo.com", null).Returns(new List<Notificacion>
        {
            new(_reservaId, "ana@ejemplo.com",
                TipoNotificacion.ReservaConfirmada, "Asunto", "Cuerpo")
        });

        var resultado = await _servicio.ListarAsync("ana@ejemplo.com", null);

        Assert.Single(resultado);
        Assert.Equal("ReservaConfirmada", resultado[0].Tipo);
        Assert.Equal("ana@ejemplo.com", resultado[0].Destinatario);
    }
}
