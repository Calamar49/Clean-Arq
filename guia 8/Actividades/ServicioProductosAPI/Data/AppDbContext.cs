using Microsoft.EntityFrameworkCore;
using ServicioProductosAPI.Models;

namespace ServicioProductosAPI.Data
{
    // Esta clase representa el contexto de base de datos.
    // Es el puente entre la aplicación y MySQL.
    public class AppDbContext : DbContext
    {
        // Constructor que recibe la configuración del contexto
        // desde el contenedor de dependencias de ASP.NET Core.
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet representa la tabla Productos.
        // A través de esta propiedad podemos consultar,
        // insertar, actualizar y eliminar registros.
        public DbSet<Producto> Productos { get; set; }
    }
}
