using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hotel.IntegrationTests;

// Pruebas de integración de la API de Habitaciones: recorren el camino completo
// controlador → servicio → repositorio → MySQL, sin dobles de prueba.
//
// Requieren MySQL/MariaDB en ejecución.
[Collection("Habitaciones")]
public class HabitacionesApiTests : IAsyncLifetime
{
    private readonly HabitacionesApiFactory _fabrica = new();
    private HttpClient _cliente = null!;

    private static readonly DateOnly Entrada = DateOnly.FromDateTime(DateTime.Today).AddDays(30);
    private static readonly DateOnly Salida = Entrada.AddDays(3);

    public async Task InitializeAsync()
    {
        _cliente = _fabrica.CreateClient();
        await _fabrica.ReiniciarBaseAsync();
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
    }

    private static object Solicitud(Guid reservaId, int habitacionId = 2,
        DateOnly? entrada = null, DateOnly? salida = null) => new
    {
        reservaId,
        habitacionId,
        fechaEntrada = (entrada ?? Entrada).ToString("yyyy-MM-dd"),
        fechaSalida = (salida ?? Salida).ToString("yyyy-MM-dd")
    };

    private Task<HttpResponseMessage> BloquearAsync(Guid reservaId, int habitacionId = 2,
        DateOnly? entrada = null, DateOnly? salida = null) =>
        _cliente.PostAsJsonAsync("api/bloqueos", Solicitud(reservaId, habitacionId, entrada, salida));

    private async Task<int> ContarDisponiblesAsync(DateOnly? entrada = null, DateOnly? salida = null)
    {
        var url = $"api/habitaciones/disponibles?entrada={(entrada ?? Entrada):yyyy-MM-dd}" +
                  $"&salida={(salida ?? Salida):yyyy-MM-dd}";

        var habitaciones = await _cliente.GetFromJsonAsync<JsonElement[]>(url);
        return habitaciones!.Length;
    }

    // ---------- Catálogo y disponibilidad ----------

    [Fact]
    public async Task Listar_DevuelveLasHabitacionesDelCatalogo()
    {
        var respuesta = await _cliente.GetAsync("api/habitaciones");
        respuesta.EnsureSuccessStatusCode();

        var habitaciones = await respuesta.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(3, habitaciones!.Length);
    }

    [Fact]
    public async Task ObtenerPorId_DevuelveNotFoundSiNoExiste()
    {
        var respuesta = await _cliente.GetAsync("api/habitaciones/999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Disponibles_FiltraPorCapacidad()
    {
        var url = $"api/habitaciones/disponibles?entrada={Entrada:yyyy-MM-dd}" +
                  $"&salida={Salida:yyyy-MM-dd}&capacidad=4";

        var habitaciones = await _cliente.GetFromJsonAsync<JsonElement[]>(url);

        // Solo la suite admite 4 personas.
        Assert.Single(habitaciones!);
    }

    [Fact]
    public async Task Disponibles_RechazaUnRangoDeFechasInvertido()
    {
        var url = $"api/habitaciones/disponibles?entrada={Salida:yyyy-MM-dd}&salida={Entrada:yyyy-MM-dd}";

        var respuesta = await _cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // ---------- Bloqueo de noches ----------

    [Fact]
    public async Task Bloquear_ReservaLasNochesYLaHabitacionDejaDeEstarDisponible()
    {
        Assert.Equal(3, await ContarDisponiblesAsync());

        var respuesta = await BloquearAsync(Guid.NewGuid());
        respuesta.EnsureSuccessStatusCode();

        Assert.Equal(2, await ContarDisponiblesAsync());
    }

    [Fact]
    public async Task Bloquear_DevuelveConflictoSiLasNochesYaEstanTomadas()
    {
        await BloquearAsync(Guid.NewGuid());

        var segunda = await BloquearAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Bloquear_DetectaElTraslapeParcialDeFechas()
    {
        await BloquearAsync(Guid.NewGuid(), entrada: Entrada, salida: Entrada.AddDays(3));

        // Se solicita un rango que se superpone con el anterior.
        var traslape = await BloquearAsync(
            Guid.NewGuid(), entrada: Entrada.AddDays(2), salida: Entrada.AddDays(5));

        Assert.Equal(HttpStatusCode.Conflict, traslape.StatusCode);
    }

    [Fact]
    public async Task Bloquear_PermiteReservarDesdeElDiaDeSalidaDeOtraReserva()
    {
        // La noche de salida queda libre: una reserva puede empezar ese día.
        await BloquearAsync(Guid.NewGuid(), entrada: Entrada, salida: Entrada.AddDays(3));

        var contigua = await BloquearAsync(
            Guid.NewGuid(), entrada: Entrada.AddDays(3), salida: Entrada.AddDays(5));

        contigua.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Bloquear_EsIdempotenteAnteUnMensajeReentregado()
    {
        var reservaId = Guid.NewGuid();

        (await BloquearAsync(reservaId)).EnsureSuccessStatusCode();
        (await BloquearAsync(reservaId)).EnsureSuccessStatusCode();

        // Se siguen bloqueando solo las 3 noches originales.
        Assert.Equal(2, await ContarDisponiblesAsync());
    }

    [Fact]
    public async Task Bloquear_DevuelveNotFoundSiLaHabitacionNoExiste()
    {
        var respuesta = await BloquearAsync(Guid.NewGuid(), habitacionId: 999);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // ---------- Compensación ----------

    [Fact]
    public async Task Liberar_DevuelveLasNochesAlaDisponibilidad()
    {
        var reservaId = Guid.NewGuid();
        await BloquearAsync(reservaId);
        Assert.Equal(2, await ContarDisponiblesAsync());

        var liberacion = await _cliente.DeleteAsync($"api/bloqueos/{reservaId}");
        liberacion.EnsureSuccessStatusCode();

        Assert.Equal(3, await ContarDisponiblesAsync());
    }

    [Fact]
    public async Task Liberar_UnaReservaSinNoches_NoProduceError()
    {
        var respuesta = await _cliente.DeleteAsync($"api/bloqueos/{Guid.NewGuid()}");

        respuesta.EnsureSuccessStatusCode();
    }

    // ---------- Concurrencia ----------

    [Fact]
    public async Task Bloquear_ConSolicitudesSimultaneas_SoloUnaTieneExito()
    {
        // Es la garantía contra el overbooking: la restricción UNIQUE de la
        // base de datos impide que dos reservas tomen la misma noche.
        const int solicitudes = 20;

        var tareas = Enumerable.Range(0, solicitudes)
            .Select(_ => BloquearAsync(Guid.NewGuid()))
            .ToArray();

        var respuestas = await Task.WhenAll(tareas);

        var exitosas = respuestas.Count(r => r.IsSuccessStatusCode);
        var conflictos = respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, exitosas);
        Assert.Equal(solicitudes - 1, conflictos);
    }
}

// Las pruebas comparten la misma base de datos, así que se ejecutan en serie.
[CollectionDefinition("Habitaciones", DisableParallelization = true)]
public class ColeccionHabitaciones;
