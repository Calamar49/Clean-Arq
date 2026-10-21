using Habitaciones.Domain.Entities;

namespace Hotel.UnitTests.Dominio;

public class HabitacionTests
{
    [Fact]
    public void AlCrearse_QuedaActivaConLosDatosIndicados()
    {
        var habitacion = new Habitacion("103", "Doble", 2, 180m);

        Assert.Equal("103", habitacion.Numero);
        Assert.Equal("Doble", habitacion.Tipo);
        Assert.Equal(2, habitacion.Capacidad);
        Assert.Equal(180m, habitacion.PrecioPorNoche);
        Assert.True(habitacion.Activa);
    }

    [Theory]
    [InlineData("simple", "Simple")]
    [InlineData("DOBLE", "Doble")]
    [InlineData("  suite  ", "Suite")]
    public void AlCrearse_NormalizaElTipoIndependientementeDeComoSeEscriba(
        string entrada, string esperado)
    {
        var habitacion = new Habitacion("201", entrada, 2, 150m);

        Assert.Equal(esperado, habitacion.Tipo);
    }

    [Fact]
    public void AlCrearse_RechazaUnTipoNoReconocido()
    {
        Assert.Throws<ArgumentException>(() => new Habitacion("103", "Penthouse", 2, 180m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AlCrearse_RechazaNumeroVacio(string numero)
    {
        Assert.Throws<ArgumentException>(() => new Habitacion(numero, "Doble", 2, 180m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void AlCrearse_RechazaCapacidadFueraDeRango(int capacidad)
    {
        Assert.Throws<ArgumentException>(() => new Habitacion("103", "Doble", capacidad, 180m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void AlCrearse_RechazaPrecioNoPositivo(decimal precio)
    {
        Assert.Throws<ArgumentException>(() => new Habitacion("103", "Doble", 2, precio));
    }

    [Fact]
    public void Actualizar_PermiteDesactivarLaHabitacion()
    {
        var habitacion = new Habitacion("103", "Doble", 2, 180m);

        habitacion.Actualizar("Suite", 4, 350m, activa: false);

        Assert.Equal("Suite", habitacion.Tipo);
        Assert.Equal(4, habitacion.Capacidad);
        Assert.Equal(350m, habitacion.PrecioPorNoche);
        Assert.False(habitacion.Activa);
    }
}

public class NocheBloqueadaTests
{
    [Fact]
    public void AlCrearse_GuardaLaHabitacionLaFechaYLaReserva()
    {
        var reservaId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 12, 20);

        var noche = new NocheBloqueada(3, fecha, reservaId);

        Assert.Equal(3, noche.HabitacionId);
        Assert.Equal(fecha, noche.Fecha);
        Assert.Equal(reservaId, noche.ReservaId);
    }

    [Fact]
    public void AlCrearse_RechazaHabitacionInvalida()
    {
        Assert.Throws<ArgumentException>(() =>
            new NocheBloqueada(0, new DateOnly(2026, 12, 20), Guid.NewGuid()));
    }

    [Fact]
    public void AlCrearse_RechazaReservaVacia()
    {
        Assert.Throws<ArgumentException>(() =>
            new NocheBloqueada(3, new DateOnly(2026, 12, 20), Guid.Empty));
    }
}
