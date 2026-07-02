using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data.Configurations;

public class HistorialActivoConfiguration : IEntityTypeConfiguration<HistorialActivo>
{
    public void Configure(EntityTypeBuilder<HistorialActivo> builder)
    {
        builder.ToTable("HistorialActivos");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.EstadoAnterior).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(h => h.EstadoNuevo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(h => h.Motivo).IsRequired().HasMaxLength(500);
        builder.Property(h => h.Fecha).IsRequired();

        // N HistorialActivo -> 1 Tecnico.
        // Restrict (NO cascade) por dos razones:
        //  1) No queremos perder la auditoría si se da de baja a un técnico.
        //  2) SQL Server prohíbe múltiples rutas de cascada hacia la misma tabla:
        //     HistorialActivos ya recibe cascada desde Activos, así que esta DEBE ser Restrict.
        builder.HasOne(h => h.Tecnico)
               .WithMany(t => t.CambiosRegistrados)
               .HasForeignKey(h => h.TecnicoId)
               .OnDelete(DeleteBehavior.Restrict);

        // La relación Activo (1) -> HistorialActivo (N) ya quedó definida en ActivoConfiguration.
    }
}
