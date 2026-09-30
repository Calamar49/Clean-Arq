using Application.Interfaces;
using Application.Services;

using Infrastructure.Repositories;

// Creamos el constructor de la aplicación ASP.NET Core.
var builder = WebApplication.CreateBuilder(args);

// Registramos el soporte para controladores.
builder.Services.AddControllers();

// Configuramos la generación de documentación Swagger.
builder.Services.AddSwaggerGen();

// Registramos la implementación del servicio.
// Se crea una instancia por ámbito de solicitud HTTP.
builder.Services.AddScoped<IClienteService, ClienteService>();

// Registramos el repositorio en memoria.
// Se comparte una instancia durante toda la ejecución.
builder.Services.AddSingleton<IClienteRepository, ClienteRepository>();

// Construimos la aplicación.
var app = builder.Build();

// Swagger estará disponible en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirigimos las solicitudes HTTP hacia HTTPS.
app.UseHttpsRedirection();

// Habilitamos los endpoints de los controladores.
app.MapControllers();

// Iniciamos el microservicio.
app.Run();
