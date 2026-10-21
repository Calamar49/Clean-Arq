using Hotel.Messaging;
using Microsoft.EntityFrameworkCore;
using Reservas.Api.Consumers;
using Reservas.Api.Filters;
using Reservas.Api.Servicios;
using Reservas.Application.Configuracion;
using Reservas.Application.Interfaces;
using Reservas.Application.Services;
using Reservas.Infrastructure.Clients;
using Reservas.Infrastructure.Persistence;
using Reservas.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Base de datos propia del servicio (db_reservas).
var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ReservasDbContext>(options =>
    options.UseMySql(conexion, ServerVersion.AutoDetect(conexion)));

// Cliente HTTP hacia el servicio de Habitaciones.
builder.Services.AddHttpClient<IHabitacionesClient, HabitacionesClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Servicios:Habitaciones"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Lado de comandos (escritura).
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();

// Lado de consultas (lectura): modelo propio alimentado por la proyección.
builder.Services.AddScoped<IReservaVistaRepository, ReservaVistaRepository>();
builder.Services.AddScoped<IProyeccionService, ProyeccionService>();

// Mensajería: publicador y consumidores (Saga y proyección, en colas separadas).
builder.Services.AgregarMensajeria(builder.Configuration);
builder.Services.AddHostedService<ReservasConsumer>();
builder.Services.AddHostedService<ProyeccionConsumer>();

// Red de seguridad: libera las habitaciones de las reservas que se quedan
// bloqueadas porque el pago nunca llega a resolverse.
builder.Services.Configure<VencimientoOptions>(
    builder.Configuration.GetSection(VencimientoOptions.Seccion));
builder.Services.AddHostedService<VencimientoDeBloqueos>();

builder.Services.AddControllers(options => options.Filters.Add<ExcepcionesFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint de salud para verificar que el servicio está activo.
app.MapGet("/health", () => Results.Ok(new { servicio = "Reservas", estado = "OK" }));

app.MapControllers();

app.Run();
