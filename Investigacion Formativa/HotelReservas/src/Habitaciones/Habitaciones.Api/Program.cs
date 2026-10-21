using Habitaciones.Api.Consumers;
using Habitaciones.Api.Filters;
using Habitaciones.Application.Interfaces;
using Habitaciones.Application.Services;
using Habitaciones.Infrastructure.Persistence;
using Habitaciones.Infrastructure.Repositories;
using Hotel.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos propia del servicio (db_habitaciones).
var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HabitacionesDbContext>(options =>
    options.UseMySql(conexion, ServerVersion.AutoDetect(conexion)));

// Inyección de dependencias: Api depende de contratos, no de implementaciones.
builder.Services.AddScoped<IHabitacionRepository, HabitacionRepository>();
builder.Services.AddScoped<IHabitacionService, HabitacionService>();

// Mensajería: publicador de eventos y consumidor de la Saga.
builder.Services.AgregarMensajeria(builder.Configuration);
builder.Services.AddHostedService<HabitacionesConsumer>();

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
app.MapGet("/health", () => Results.Ok(new { servicio = "Habitaciones", estado = "OK" }));

app.MapControllers();

app.Run();
