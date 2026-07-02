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
