using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data.Configurations;

public class ActivoConfiguration : IEntityTypeConfiguration<Activo>
{
    public void Configure(EntityTypeBuilder<Activo> builder)
    {
        builder.ToTable("Activos");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(a => a.NumeroSerie).IsRequired().HasMaxLength(100);

        // Guardamos los enums como texto en la BD (más legible que un int al inspeccionar la tabla).
        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Solo fecha (sin hora) para la adquisición.
        builder.Property(a => a.FechaAdquisicion).HasColumnType("date");

        builder.HasIndex(a => a.NumeroSerie).IsUnique();

        // Índices de rendimiento (medidos con datos de carga):
        //  - Estado: el dashboard hace GROUP BY Estado (scan de tabla completa sin esto).
        //  - Nombre: la paginación ordena por Nombre; sin índice, ordena TODA la tabla por página.
        builder.HasIndex(a => a.Estado);
        builder.HasIndex(a => a.Nombre);

        // 1 Activo -> N HistorialActivo.
        // Cascade: si se elimina el activo, su historial de auditoría se va con él.
        // Es la ÚNICA ruta de borrado en cascada hacia HistorialActivos (ver nota en HistorialActivoConfiguration).
        builder.HasMany(a => a.Historial)
               .WithOne(h => h.Activo)
               .HasForeignKey(h => h.ActivoId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
