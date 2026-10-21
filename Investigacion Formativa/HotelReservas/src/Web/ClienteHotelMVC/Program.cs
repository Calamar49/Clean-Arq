using ClienteHotelMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Clientes HTTP hacia los microservicios.
// La aplicación web nunca accede a las bases de datos: todo pasa por las APIs.
builder.Services.AddHttpClient<HabitacionesApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Servicios:Habitaciones"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<ReservasApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Servicios:Reservas"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Reservas/Error");
    app.UseHsts();
}

// Al publicar en la red del aula se sirve solo por HTTP: el certificado de
// desarrollo no es de confianza en otras máquinas, y redirigir a HTTPS les
// mostraría una advertencia de seguridad en lugar de la aplicación.
if (!builder.Configuration.GetValue<bool>("SoloHttp"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reservas}/{action=Index}/{id?}");

app.Run();
