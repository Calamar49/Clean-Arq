using Hotel.Contracts;
using NSubstitute;
using Reservas.Application.DTOs;
using Reservas.Application.Exceptions;
using Reservas.Application.Interfaces;
using Reservas.Application.Services;
using Reservas.Domain.Entities;

namespace Hotel.UnitTests.Aplicacion;

// Verifica que el servicio orqueste correctamente su parte de la Saga:
// consulte el precio al servicio dueño del dato y publique los eventos debidos.
public class ReservaServiceTests
{
    private readonly IReservaRepository _repositorio = Substitute.For<IReservaRepository>();
    private readonly IHabitacionesClient _habitaciones = Substitute.For<IHabitacionesClient>();
    private readonly IEventPublisher _eventos = Substitute.For<IEventPublisher>();
    private readonly ReservaService _servicio;

    private static readonly DateOnly Entrada = DateOnly.FromDateTime(DateTime.Today).AddDays(10);
    private static readonly DateOnly Salida = Entrada.AddDays(3);

    public ReservaServiceTests()
    {
        _servicio = new ReservaService(_repositorio, _habitaciones, _eventos);
        _repositorio.GuardarCambiosAsync().Returns(true);
    }

    private void ConfigurarHabitacion(bool activa = true, decimal precio = 180m)
    {
        _habitaciones.ObtenerHabitacionAsync(Arg.Any<int>()).Returns(new HabitacionInfoDTO
        {
            Id = 3,
            Numero = "103",
            Tipo = "Doble",
            Capacidad = 2,
            PrecioPorNoche = precio,
            Activa = activa
        });
    }

    private static CrearReservaDTO DatosValidos() => new()
    {
        HabitacionId = 3,
        HuespedNombre = "Manuel Cuchuyrumi",
        HuespedEmail = "manuel@ejemplo.com",
        FechaEntrada = Entrada,
        FechaSalida = Salida,
        MetodoPago = MetodosPago.TarjetaAprobada
    };

    private Reserva ReservaExistente(Action<Reserva>? preparar = null)
    {
        var reserva = new Reserva(3, "103", "Manuel", "manuel@ejemplo.com", Entrada, Salida, 180m, MetodosPago.TarjetaAprobada);
        preparar?.Invoke(reserva);
        _repositorio.ObtenerAsync(Arg.Any<Guid>()).Returns(reserva);
        return reserva;
    }

    // ---------- Creación ----------

    [Fact]
    public async Task Crear_TomaElPrecioDelServicioDeHabitaciones()
    {
        // El cliente nunca envía el precio: se consulta al servicio dueño del dato.
        ConfigurarHabitacion(precio: 210m);

        var resultado = await _servicio.CrearAsync(DatosValidos());

        Assert.Equal(210m, resultado.PrecioPorNoche);
        Assert.Equal(630m, resultado.MontoTotal);
        Assert.Equal("103", resultado.HabitacionNumero);
    }

    [Fact]
    public async Task Crear_PublicaElEventoQueIniciaLaSaga()
    {
        ConfigurarHabitacion();

        var resultado = await _servicio.CrearAsync(DatosValidos());

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<ReservaSolicitada>(e => e.ReservaId == resultado.Id && e.HabitacionId == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Crear_GuardaLaReservaAntesDePublicar()
    {
        ConfigurarHabitacion();

        await _servicio.CrearAsync(DatosValidos());

        await _repositorio.Received(1).AgregarAsync(Arg.Any<Reserva>());
    }

    [Fact]
    public async Task Crear_FallaSiLaHabitacionNoExiste()
    {
        _habitaciones.ObtenerHabitacionAsync(Arg.Any<int>()).Returns((HabitacionInfoDTO?)null);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => _servicio.CrearAsync(DatosValidos()));
    }

    [Fact]
    public async Task Crear_FallaSiLaHabitacionEstaInactiva()
    {
        ConfigurarHabitacion(activa: false);

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.CrearAsync(DatosValidos()));
    }

    [Fact]
    public async Task Crear_NoPublicaNingunEventoSiFallaLaValidacion()
    {
        ConfigurarHabitacion(activa: false);

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.CrearAsync(DatosValidos()));

        await _eventos.DidNotReceive().PublicarAsync(
            Arg.Any<ReservaSolicitada>(), Arg.Any<CancellationToken>());
    }

    // ---------- Transiciones de la Saga ----------

    [Fact]
    public async Task MarcarBloqueada_PublicaElEventoQueDisparaElCobro()
    {
        var reserva = ReservaExistente();

        await _servicio.MarcarBloqueadaAsync(reserva.Id);

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<ReservaBloqueada>(e => e.ReservaId == reserva.Id && e.MontoTotal == 540m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmar_PublicaElEventoDeReservaConfirmada()
    {
        var reserva = ReservaExistente(r => r.MarcarBloqueada());

        await _servicio.ConfirmarAsync(reserva.Id);

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<ReservaConfirmada>(e => e.ReservaId == reserva.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rechazar_PublicaElEventoQueDisparaLaCompensacion()
    {
        // Es el evento que hace que Habitaciones libere las noches bloqueadas.
        var reserva = ReservaExistente(r => r.MarcarBloqueada());

        await _servicio.RechazarAsync(reserva.Id, "La tarjeta fue rechazada.");

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<ReservaRechazada>(e =>
                e.ReservaId == reserva.Id && e.Motivo == "La tarjeta fue rechazada."),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancelar_PublicaElEventoDeCancelacion()
    {
        var reserva = ReservaExistente(r => r.MarcarBloqueada());

        await _servicio.CancelarAsync(reserva.Id, new CancelarReservaDTO { Motivo = "Cambio de planes" });

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<ReservaCancelada>(e => e.ReservaId == reserva.Id),
            Arg.Any<CancellationToken>());
    }

    // ---------- Concurrencia y errores ----------

    [Fact]
    public async Task Transicion_LanzaConflictoSiOtroProcesoModificoLaReserva()
    {
        // Simula el fallo de la concurrencia optimista.
        ReservaExistente();
        _repositorio.GuardarCambiosAsync().Returns(false);

        await Assert.ThrowsAsync<ConflictoException>(
            () => _servicio.MarcarBloqueadaAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Transicion_FallaSiLaReservaNoExiste()
    {
        _repositorio.ObtenerAsync(Arg.Any<Guid>()).Returns((Reserva?)null);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => _servicio.ConfirmarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Listar_RechazaUnEstadoQueNoExiste()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.ListarAsync(null, "EstadoInventado"));
    }

    [Fact]
    public async Task Listar_AceptaLosEstadosValidos()
    {
        _repositorio.ListarAsync(Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(new List<Reserva>());

        var resultado = await _servicio.ListarAsync("a@b.com", "Confirmada");

        Assert.Empty(resultado);
    }

    // ---------- Vencimiento de bloqueos ----------
    //
    // Si el servicio de Pagos no responde, nadie publica el evento que libera
    // las noches. Esta revisión por tiempo es la red de seguridad para ese caso.

    private static readonly TimeSpan Limite = TimeSpan.FromMinutes(15);

    // Registra una reserva en el estado indicado y la expone por su propio Id,
    // de modo que un mismo lote pueda contener reservas en estados distintos.
    private Reserva ReservaRegistrada(Action<Reserva>? preparar = null)
    {
        var reserva = new Reserva(
            3, "103", "Manuel", "manuel@ejemplo.com", Entrada, Salida, 180m, MetodosPago.TarjetaAprobada);

        preparar?.Invoke(reserva);
        _repositorio.ObtenerAsync(reserva.Id).Returns(reserva);
        return reserva;
    }

    private void ConfigurarLote(params Reserva[] reservas)
    {
        _repositorio.ListarBloqueadasAntesDeAsync(Arg.Any<DateTime>(), Arg.Any<int>())
            .Returns(reservas.Select(r => r.Id).ToList());
    }

    [Fact]
    public async Task VencerBloqueos_RechazaLasReservasQueLlevanDemasiadoTiempoBloqueadas()
    {
        var primera = ReservaRegistrada(r => r.MarcarBloqueada());
        var segunda = ReservaRegistrada(r => r.MarcarBloqueada());
        ConfigurarLote(primera, segunda);

        var liberadas = await _servicio.VencerBloqueosAsync(Limite);

        Assert.Equal(2, liberadas);
        Assert.Equal(EstadoReserva.Rechazada, primera.Estado);
        Assert.Equal(EstadoReserva.Rechazada, segunda.Estado);
    }

    [Fact]
    public async Task VencerBloqueos_PublicaElEventoQueLiberaLasNoches()
    {
        // Es el mismo evento que usa la compensación normal de la Saga:
        // Habitaciones no necesita saber que el rechazo vino de un vencimiento.
        var reserva = ReservaRegistrada(r => r.MarcarBloqueada());
        ConfigurarLote(reserva);

        await _servicio.VencerBloqueosAsync(Limite);

        await _eventos.Received(1).PublicarAsync(
            Arg.Is<ReservaRechazada>(e =>
                e.ReservaId == reserva.Id && e.Motivo == ReservaService.MotivoVencimiento),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VencerBloqueos_ConsultaUsandoLaFechaDeCorteDelLimite()
    {
        ConfigurarLote();

        await _servicio.VencerBloqueosAsync(Limite);

        await _repositorio.Received(1).ListarBloqueadasAntesDeAsync(
            Arg.Is<DateTime>(corte =>
                corte <= DateTime.Now.AddMinutes(-15) && corte > DateTime.Now.AddMinutes(-16)),
            Arg.Any<int>());
    }

    [Fact]
    public async Task VencerBloqueos_NoPublicaNadaCuandoNoHayBloqueosVencidos()
    {
        ConfigurarLote();

        var liberadas = await _servicio.VencerBloqueosAsync(Limite);

        Assert.Equal(0, liberadas);
        await _eventos.DidNotReceive().PublicarAsync(
            Arg.Any<ReservaRechazada>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VencerBloqueos_OmiteLaReservaQueYaCambioDeEstadoYSigueConLasDemas()
    {
        // El pago llegó justo entre la consulta y el rechazo: esa reserva ya no
        // admite la transición, pero el resto del lote debe procesarse igual.
        var resuelta = ReservaRegistrada(r => { r.MarcarBloqueada(); r.Confirmar(); });
        var vencida = ReservaRegistrada(r => r.MarcarBloqueada());
        ConfigurarLote(resuelta, vencida);

        var liberadas = await _servicio.VencerBloqueosAsync(Limite);

        Assert.Equal(1, liberadas);
        Assert.Equal(EstadoReserva.Confirmada, resuelta.Estado);
        Assert.Equal(EstadoReserva.Rechazada, vencida.Estado);
    }

    [Fact]
    public async Task VencerBloqueos_NoCuentaLaQueOtroProcesoModificoPrimero()
    {
        var reserva = ReservaRegistrada(r => r.MarcarBloqueada());
        ConfigurarLote(reserva);
        _repositorio.GuardarCambiosAsync().Returns(false);

        var liberadas = await _servicio.VencerBloqueosAsync(Limite);

        Assert.Equal(0, liberadas);
        await _eventos.DidNotReceive().PublicarAsync(
            Arg.Any<ReservaRechazada>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VencerBloqueos_SeDetieneSiSeCancelaLaRevision()
    {
        var primera = ReservaRegistrada(r => r.MarcarBloqueada());
        var segunda = ReservaRegistrada(r => r.MarcarBloqueada());
        ConfigurarLote(primera, segunda);

        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        var liberadas = await _servicio.VencerBloqueosAsync(Limite, cancelacion.Token);

        Assert.Equal(0, liberadas);
    }
}
