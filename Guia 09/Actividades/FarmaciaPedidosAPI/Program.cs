var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger queda habilitado también dentro del contenedor: la guía pide
// registrar un pedido desde Swagger y volver a listar.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
