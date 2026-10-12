using Microsoft.EntityFrameworkCore;
using ServicioCatalogoAPI.Models;

namespace ServicioCatalogoAPI.Data
{
    // Contexto de base de datos: puente entre la API y MySQL.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Representa la tabla Productos.
        public DbSet<Producto> Productos { get; set; }
    }
}
