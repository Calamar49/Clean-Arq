using Hotel.Messaging;
using Microsoft.EntityFrameworkCore;
using Notificaciones.Api.Consumers;
using Notificaciones.Application.Interfaces;
using Notificaciones.Application.Services;
using Notificaciones.Infrastructure.Persistence;
using Notificaciones.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Base de datos propia del servicio (db_notificaciones).
var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NotificacionesDbContext>(options =>
    options.UseMySql(conexion, ServerVersion.AutoDetect(conexion)));

builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();

// Mensajería: este servicio solo consume eventos, no publica.
builder.Services.AgregarMensajeria(builder.Configuration);
builder.Services.AddHostedService<NotificacionesConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { servicio = "Notificaciones", estado = "OK" }));

app.MapControllers();

app.Run();
