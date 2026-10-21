using Habitaciones.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hotel.IntegrationTests;

// Levanta el servicio de Habitaciones en memoria, pero contra una base de datos
// MySQL real y separada (db_habitaciones_test), para no alterar los datos de
// la demostración. Los consumidores de RabbitMQ se desactivan: estas pruebas
// verifican la API y la persistencia, no la mensajería.
public class HabitacionesApiFactory : WebApplicationFactory<Program>
{
    public const string CadenaConexion =
        "server=localhost;port=3306;database=db_habitaciones_test;user=root;password=";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(servicios =>
        {
            // Se sustituye el contexto por uno apuntando a la base de pruebas.
            var descriptor = servicios.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<HabitacionesDbContext>));

            if (descriptor is not null)
                servicios.Remove(descriptor);

            servicios.AddDbContext<HabitacionesDbContext>(opciones =>
                opciones.UseMySql(CadenaConexion, ServerVersion.AutoDetect(CadenaConexion)));

            // Se retiran los servicios en segundo plano (consumidores de RabbitMQ).
            foreach (var hosted in servicios.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                servicios.Remove(hosted);
        });
    }

    // Deja la base de pruebas en un estado conocido antes de cada prueba.
    public async Task ReiniciarBaseAsync()
    {
        using var ambito = Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<HabitacionesDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        db.Habitaciones.AddRange(
            new Habitaciones.Domain.Entities.Habitacion("101", "Simple", 1, 120m),
            new Habitaciones.Domain.Entities.Habitacion("103", "Doble", 2, 180m),
            new Habitaciones.Domain.Entities.Habitacion("202", "Suite", 4, 350m));

        await db.SaveChangesAsync();
    }
}
