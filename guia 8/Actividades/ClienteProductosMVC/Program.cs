var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Registrar servicios MVC
// ---------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------
// 2. Registrar HttpClient para consumir la API
// ---------------------------------------------------------
// BaseAddress debe apuntar a la URL del proyecto API.
// Esta URL puede variar según el equipo del estudiante.
builder.Services.AddHttpClient("ProductosAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7231/");
});

var app = builder.Build();

// ---------------------------------------------------------
// 3. Configuración del pipeline HTTP
// ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Productos}/{action=Index}/{id?}");

app.Run();
