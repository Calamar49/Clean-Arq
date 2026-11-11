var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger queda habilitado también dentro del contenedor: la guía pide
// comprobar los endpoints desde el navegador, y la imagen se ejecuta con
// el entorno Production por defecto.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
