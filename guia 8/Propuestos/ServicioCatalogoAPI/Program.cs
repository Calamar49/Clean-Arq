using Microsoft.EntityFrameworkCore;
using ServicioCatalogoAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Registrar el contexto de base de datos (MySQL)
// ---------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// ---------------------------------------------------------
// 2. Registrar servicios de la API
// ---------------------------------------------------------
builder.Services.AddControllers();
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
app.MapControllers();

app.Run();
