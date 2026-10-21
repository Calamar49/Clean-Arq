using Microsoft.EntityFrameworkCore;
using Pagos.Domain.Entities;

namespace Pagos.Infrastructure.Persistence
{
    public class PagosDbContext : DbContext
    {
        public PagosDbContext(DbContextOptions<PagosDbContext> options) : base(options)
        {
        }

        public DbSet<Pago> Pagos => Set<Pago>();

        public DbSet<MensajeProcesado> MensajesProcesados => Set<MensajeProcesado>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pago>(e =>
            {
                e.ToTable("Pagos");
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).HasColumnType("char(36)");
                e.Property(p => p.ReservaId).HasColumnType("char(36)");
                e.Property(p => p.Monto).HasPrecision(10, 2);
                e.Property(p => p.Moneda).HasMaxLength(3).IsRequired();
                e.Property(p => p.Pasarela).HasMaxLength(30).IsRequired();
                e.Property(p => p.Referencia).HasMaxLength(120);
                e.Property(p => p.Motivo).HasMaxLength(250);
                e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

                // Una reserva no puede tener dos pagos.
                e.HasIndex(p => p.ReservaId).IsUnique();
            });

            modelBuilder.Entity<MensajeProcesado>(e =>
            {
                e.ToTable("MensajesProcesados");
                e.HasKey(m => m.MessageId);
                e.Property(m => m.MessageId).HasColumnType("char(36)");
                e.Property(m => m.RoutingKey).HasMaxLength(60).IsRequired();
            });
        }
    }
}
