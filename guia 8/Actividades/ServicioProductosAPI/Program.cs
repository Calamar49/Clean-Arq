using Microsoft.EntityFrameworkCore;
using ServicioProductosAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Registrar el contexto de base de datos
// ---------------------------------------------------------
// Aquí se configura Entity Framework Core para usar MySQL.
// ServerVersion.AutoDetect detecta automáticamente la versión
// del servidor MySQL instalado.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// ---------------------------------------------------------
// 2. Registrar servicios de la API
// ---------------------------------------------------------
builder.Services.AddControllers();

// Habilita Swagger para pruebas rápidas del microservicio.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------
// 3. Configurar pipeline HTTP
// ---------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Mapear controladores
app.MapControllers();

app.Run();
