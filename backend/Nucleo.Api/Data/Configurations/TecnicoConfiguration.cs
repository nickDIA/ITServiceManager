using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data.Configurations;

public class TecnicoConfiguration : IEntityTypeConfiguration<Tecnico>
{
    public void Configure(EntityTypeBuilder<Tecnico> builder)
    {
        builder.ToTable("Tecnicos");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Email).IsRequired().HasMaxLength(256);
        builder.Property(t => t.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Email único: será el identificador de login.
        builder.HasIndex(t => t.Email).IsUnique();
    }
}
