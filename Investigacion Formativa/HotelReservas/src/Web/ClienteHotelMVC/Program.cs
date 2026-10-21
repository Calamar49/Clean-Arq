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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reservas}/{action=Index}/{id?}");

app.Run();
