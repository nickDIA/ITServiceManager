using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Descripcion).IsRequired().HasMaxLength(2000);
        builder.Property(t => t.Prioridad).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.FechaCreacion).IsRequired();

        // Índices de rendimiento (medidos con datos de carga).
        //  - (Estado, FechaCreacion): sirve DOS patrones con un solo índice.
        //      a) el kanban paginado por columna: WHERE Estado = X ORDER BY FechaCreacion DESC
        //         -> seek + top-N, sin ordenar las ~40k filas del estado.
        //      b) el GROUP BY Estado del dashboard (Estado es la columna líder).
        //  - Prioridad: el GROUP BY Prioridad del dashboard.
        // Sin ellos, cada agregación era un scan de tabla completa (~200k filas).
        builder.HasIndex(t => new { t.Estado, t.FechaCreacion });
        builder.HasIndex(t => t.Prioridad);

        // Ticket (N) -> Activo (0..1). Opcional: ActivoId es nullable.
        // SetNull: si se elimina el activo, el ticket queda sin activo en vez de bloquear o borrarse.
        builder.HasOne(t => t.Activo)
               .WithMany() // Activo no necesita colección inversa de tickets.
               .HasForeignKey(t => t.ActivoId)
               .OnDelete(DeleteBehavior.SetNull);

        // Ticket (N) -> Tecnico (1). Restrict: no borrar técnicos con tickets asignados.
        builder.HasOne(t => t.Tecnico)
               .WithMany(te => te.Tickets)
               .HasForeignKey(t => t.TecnicoId)
               .OnDelete(DeleteBehavior.Restrict);

        // La relación Cliente (1) -> Ticket (N) ya quedó definida en ClienteConfiguration.
    }
}
