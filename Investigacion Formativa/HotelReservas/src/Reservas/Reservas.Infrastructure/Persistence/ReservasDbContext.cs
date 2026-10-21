using Microsoft.EntityFrameworkCore;
using Reservas.Domain.Entities;

namespace Reservas.Infrastructure.Persistence
{
    // Contexto de EF Core para la base de datos propia db_reservas.
    public class ReservasDbContext : DbContext
    {
        public ReservasDbContext(DbContextOptions<ReservasDbContext> options)
            : base(options)
        {
        }

        // Modelo de escritura (lado "command").
        public DbSet<Reserva> Reservas => Set<Reserva>();

        // Modelo de lectura (lado "query"), alimentado por la proyección.
        public DbSet<ReservaVista> ReservasVista => Set<ReservaVista>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reserva>(e =>
            {
                e.ToTable("Reservas");
                e.HasKey(r => r.Id);

                e.Property(r => r.Id).HasColumnType("char(36)");
                e.Property(r => r.HabitacionNumero).HasMaxLength(10).IsRequired();
                e.Property(r => r.HuespedNombre).HasMaxLength(120).IsRequired();
                e.Property(r => r.HuespedEmail).HasMaxLength(150).IsRequired();
                e.Property(r => r.FechaEntrada).HasColumnType("date");
                e.Property(r => r.FechaSalida).HasColumnType("date");
                e.Property(r => r.PrecioPorNoche).HasPrecision(10, 2);
                e.Property(r => r.MontoTotal).HasPrecision(10, 2);
                e.Property(r => r.MetodoPago).HasMaxLength(40).IsRequired();
                e.Property(r => r.Motivo).HasMaxLength(250);

                // El estado se guarda como texto para que sea legible en la base de datos.
                e.Property(r => r.Estado)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                // Concurrencia optimista: EF Core compara el valor original de Version
                // en el WHERE del UPDATE. Si otro proceso ya lo cambió, la operación falla.
                e.Property(r => r.Version).IsConcurrencyToken();

                e.HasIndex(r => r.Estado);
                e.HasIndex(r => r.HuespedEmail);
                e.HasIndex(r => r.HabitacionId);
            });

            modelBuilder.Entity<ReservaVista>(e =>
            {
                e.ToTable("ReservasVista");
                e.HasKey(v => v.Id);

                e.Property(v => v.Id).HasColumnType("char(36)");
                e.Property(v => v.HabitacionNumero).HasMaxLength(10).IsRequired();
                e.Property(v => v.HuespedNombre).HasMaxLength(120).IsRequired();
                e.Property(v => v.HuespedEmail).HasMaxLength(150).IsRequired();
                e.Property(v => v.FechaEntrada).HasColumnType("date");
                e.Property(v => v.FechaSalida).HasColumnType("date");
                e.Property(v => v.MontoTotal).HasPrecision(10, 2);
                e.Property(v => v.Estado).HasMaxLength(20).IsRequired();
                e.Property(v => v.Motivo).HasMaxLength(250);

                e.HasIndex(v => v.HuespedEmail);
                e.HasIndex(v => v.Estado);
            });
        }
    }
}
