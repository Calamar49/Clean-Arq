var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Registrar servicios MVC
// ---------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------
// 2. Registrar HttpClient para consumir el microservicio
// ---------------------------------------------------------
// BaseAddress apunta a la URL de ServicioCatalogoAPI.
builder.Services.AddHttpClient("CatalogoAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
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
