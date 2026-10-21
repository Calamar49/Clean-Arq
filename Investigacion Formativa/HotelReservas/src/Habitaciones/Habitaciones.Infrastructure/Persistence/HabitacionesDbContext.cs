using Habitaciones.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Habitaciones.Infrastructure.Persistence
{
    // Contexto de EF Core para la base de datos propia db_habitaciones.
    public class HabitacionesDbContext : DbContext
    {
        public HabitacionesDbContext(DbContextOptions<HabitacionesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Habitacion> Habitaciones => Set<Habitacion>();

        public DbSet<NocheBloqueada> NochesBloqueadas => Set<NocheBloqueada>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Habitacion>(e =>
            {
                e.ToTable("Habitaciones");
                e.HasKey(h => h.Id);
                e.Property(h => h.Numero).HasMaxLength(10).IsRequired();
                e.Property(h => h.Tipo).HasMaxLength(20).IsRequired();
                e.Property(h => h.PrecioPorNoche).HasPrecision(10, 2);
                e.HasIndex(h => h.Numero).IsUnique();
            });

            modelBuilder.Entity<NocheBloqueada>(e =>
            {
                e.ToTable("NochesBloqueadas");
                e.HasKey(n => n.Id);
                e.Property(n => n.Fecha).HasColumnType("date");

                // Garantía contra el overbooking: una noche de una habitación
                // solo puede estar bloqueada por una reserva.
                e.HasIndex(n => new { n.HabitacionId, n.Fecha })
                    .IsUnique()
                    .HasDatabaseName("uq_habitacion_fecha");

                e.HasOne<Habitacion>()
                    .WithMany()
                    .HasForeignKey(n => n.HabitacionId);
            });
        }
    }
}
