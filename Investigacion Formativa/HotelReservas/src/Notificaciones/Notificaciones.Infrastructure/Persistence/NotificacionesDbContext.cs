using Microsoft.EntityFrameworkCore;
using Notificaciones.Domain.Entities;

namespace Notificaciones.Infrastructure.Persistence
{
    public class NotificacionesDbContext : DbContext
    {
        public NotificacionesDbContext(DbContextOptions<NotificacionesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

        public DbSet<MensajeProcesado> MensajesProcesados => Set<MensajeProcesado>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notificacion>(e =>
            {
                e.ToTable("Notificaciones");
                e.HasKey(n => n.Id);
                e.Property(n => n.Id).HasColumnType("char(36)");
                e.Property(n => n.ReservaId).HasColumnType("char(36)");
                e.Property(n => n.Destinatario).HasMaxLength(150).IsRequired();
                e.Property(n => n.Asunto).HasMaxLength(150).IsRequired();
                e.Property(n => n.Cuerpo).HasColumnType("text").IsRequired();
                e.Property(n => n.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();

                e.HasIndex(n => n.ReservaId);
                e.HasIndex(n => n.Destinatario);
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
