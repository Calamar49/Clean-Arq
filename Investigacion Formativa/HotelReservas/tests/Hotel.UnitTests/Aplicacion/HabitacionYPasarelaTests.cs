using Hotel.Contracts;
using Habitaciones.Application.DTOs;
using Habitaciones.Application.Exceptions;
using Habitaciones.Application.Interfaces;
using Habitaciones.Application.Services;
using Habitaciones.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Pagos.Application.Configuracion;
using Pagos.Infrastructure.Pasarelas;

namespace Hotel.UnitTests.Aplicacion;

public class HabitacionServiceTests
{
    private readonly IHabitacionRepository _repositorio = Substitute.For<IHabitacionRepository>();
    private readonly HabitacionService _servicio;

    private static readonly DateOnly Entrada = DateOnly.FromDateTime(DateTime.Today).AddDays(10);
    private static readonly DateOnly Salida = Entrada.AddDays(3);

    public HabitacionServiceTests()
    {
        _servicio = new HabitacionService(_repositorio);
    }

    private void ConfigurarHabitacion(bool activa = true)
    {
        var habitacion = new Habitacion("103", "Doble", 2, 180m);
        if (!activa) habitacion.Actualizar("Doble", 2, 180m, activa: false);

        _repositorio.ObtenerAsync(Arg.Any<int>()).Returns(habitacion);
    }

    private static BloquearNochesDTO Solicitud(DateOnly? entrada = null, DateOnly? salida = null) => new()
    {
        ReservaId = Guid.NewGuid(),
        HabitacionId = 3,
        FechaEntrada = entrada ?? Entrada,
        FechaSalida = salida ?? Salida
    };

    [Fact]
    public async Task Bloquear_DevuelveBloqueadoCuandoLasNochesEstanLibres()
    {
        ConfigurarHabitacion();
        _repositorio.BloquearNochesAsync(
                Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>())
            .Returns(ResultadoBloqueo.Bloqueado);

        var resultado = await _servicio.BloquearNochesAsync(Solicitud());

        Assert.Equal(ResultadoBloqueo.Bloqueado, resultado);
    }

    [Fact]
    public async Task Bloquear_DevuelveNoDisponibleCuandoAlgunaNocheEstaOcupada()
    {
        ConfigurarHabitacion();
        _repositorio.BloquearNochesAsync(
                Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>())
            .Returns(ResultadoBloqueo.NoDisponible);

        var resultado = await _servicio.BloquearNochesAsync(Solicitud());

        Assert.Equal(ResultadoBloqueo.NoDisponible, resultado);
    }

    [Fact]
    public async Task Bloquear_FallaSiLaHabitacionNoExiste()
    {
        _repositorio.ObtenerAsync(Arg.Any<int>()).Returns((Habitacion?)null);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => _servicio.BloquearNochesAsync(Solicitud()));
    }

    [Fact]
    public async Task Bloquear_FallaSiLaHabitacionEstaInactiva()
    {
        ConfigurarHabitacion(activa: false);

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.BloquearNochesAsync(Solicitud()));
    }

    [Fact]
    public async Task Bloquear_RechazaUnRangoDeFechasInvertido()
    {
        ConfigurarHabitacion();

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.BloquearNochesAsync(Solicitud(Salida, Entrada)));
    }

    [Fact]
    public async Task Bloquear_RechazaFechasEnElPasado()
    {
        ConfigurarHabitacion();
        var ayer = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.BloquearNochesAsync(Solicitud(ayer, ayer.AddDays(2))));
    }

    [Fact]
    public async Task Bloquear_RechazaUnaReservaSinIdentificador()
    {
        var solicitud = Solicitud();
        solicitud.ReservaId = Guid.Empty;

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.BloquearNochesAsync(solicitud));
    }

    [Fact]
    public async Task ConsultarDisponibles_RechazaUnRangoInvertido()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.ObtenerDisponiblesAsync(Salida, Entrada, null));
    }

    [Fact]
    public async Task Liberar_RechazaUnaReservaSinIdentificador()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => _servicio.LiberarReservaAsync(Guid.Empty));
    }

    [Fact]
    public async Task Crear_FallaSiElNumeroDeHabitacionYaExiste()
    {
        _repositorio.ExisteNumeroAsync("103").Returns(true);

        await Assert.ThrowsAsync<ConflictoException>(() =>
            _servicio.CrearAsync(new CrearHabitacionDTO
            {
                Numero = "103", Tipo = "Doble", Capacidad = 2, PrecioPorNoche = 180m
            }));
    }
}

// La pasarela simulada permite demostrar la compensación de forma determinista.
public class PasarelaSimuladaTests
{
    private static PasarelaSimulada Crear(ModoSimulado modo)
    {
        var opciones = Options.Create(new PasarelaOptions
        {
            Simulada = new SimuladaOptions { Modo = modo, RetrasoMs = 0 }
        });

        return new PasarelaSimulada(opciones, NullLogger<PasarelaSimulada>.Instance);
    }

    [Fact]
    public async Task TarjetaAprobada_DevuelveUnCobroAprobado()
    {
        var pasarela = Crear(ModoSimulado.AprobarSiempre);

        var resultado = await pasarela.CobrarAsync(
            540m, "mxn", "Reserva", MetodosPago.TarjetaAprobada);

        Assert.True(resultado.Aprobado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Referencia));
        Assert.Null(resultado.Motivo);
    }

    [Fact]
    public async Task TarjetaRechazada_DevuelveUnCobroRechazadoAunqueElModoApruebe()
    {
        // La tarjeta elegida por el huésped manda sobre la configuración:
        // es lo que permite demostrar la compensación desde la interfaz.
        var pasarela = Crear(ModoSimulado.AprobarSiempre);

        var resultado = await pasarela.CobrarAsync(
            540m, "mxn", "Reserva", MetodosPago.TarjetaRechazada);

        Assert.False(resultado.Aprobado);
        Assert.Null(resultado.Referencia);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Motivo));
    }

    [Fact]
    public async Task ModoRechazarSiempre_RechazaAunqueLaTarjetaSeaValida()
    {
        var pasarela = Crear(ModoSimulado.RechazarSiempre);

        var resultado = await pasarela.CobrarAsync(
            540m, "mxn", "Reserva", MetodosPago.TarjetaAprobada);

        Assert.False(resultado.Aprobado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Motivo));
    }

    [Fact]
    public void SeIdentificaComoSimulada()
    {
        Assert.Equal("Simulada", Crear(ModoSimulado.AprobarSiempre).Nombre);
    }
}
