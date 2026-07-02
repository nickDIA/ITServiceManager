using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Rfc).IsRequired().HasMaxLength(13);
        builder.Property(c => c.Contacto).HasMaxLength(200);
        builder.Property(c => c.Telefono).HasMaxLength(20);
        builder.Property(c => c.Activo).HasDefaultValue(true);

        // RFC único en todo el sistema.
        builder.HasIndex(c => c.Rfc).IsUnique();

        // 1 Cliente -> N Activos.
        // Restrict: no se puede borrar un cliente que aún tiene activos (lo validamos en el service).
        builder.HasMany(c => c.Activos)
               .WithOne(a => a.Cliente)
               .HasForeignKey(a => a.ClienteId)
               .OnDelete(DeleteBehavior.Restrict);

        // 1 Cliente -> N Tickets.
        builder.HasMany(c => c.Tickets)
               .WithOne(t => t.Cliente)
               .HasForeignKey(t => t.ClienteId)
               .OnDelete(DeleteBehavior.Restrict);

        // 1 Cliente -> N Contratos (histórico de contratos del cliente).
        builder.HasMany(c => c.Contratos)
               .WithOne(co => co.Cliente)
               .HasForeignKey(co => co.ClienteId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
