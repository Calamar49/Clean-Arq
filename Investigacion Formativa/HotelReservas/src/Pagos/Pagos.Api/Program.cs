using Hotel.Messaging;
using Microsoft.EntityFrameworkCore;
using Pagos.Api.Consumers;
using Pagos.Application.Configuracion;
using Pagos.Application.Interfaces;
using Pagos.Application.Services;
using Pagos.Infrastructure.Pasarelas;
using Pagos.Infrastructure.Persistence;
using Pagos.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Base de datos propia del servicio (db_pagos).
var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PagosDbContext>(options =>
    options.UseMySql(conexion, ServerVersion.AutoDetect(conexion)));

builder.Services.Configure<PasarelaOptions>(
    builder.Configuration.GetSection(PasarelaOptions.Seccion));

// Selección de la pasarela según la configuración.
// La lógica de negocio solo conoce la interfaz IPasarelaPago, de modo que
// cambiar de proveedor no obliga a tocar el servicio de aplicación.
var proveedor = builder.Configuration[$"{PasarelaOptions.Seccion}:Proveedor"] ?? "Simulada";

if (proveedor.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IPasarelaPago, PasarelaStripe>();
else
    builder.Services.AddScoped<IPasarelaPago, PasarelaSimulada>();

builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IPagoService, PagoService>();

// Mensajería: publicador de eventos y consumidor de la Saga.
builder.Services.AgregarMensajeria(builder.Configuration);
builder.Services.AddHostedService<PagosConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { servicio = "Pagos", estado = "OK" }));

app.MapControllers();

app.Run();
