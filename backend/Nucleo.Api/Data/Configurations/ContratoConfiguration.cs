using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("Contratos");
        builder.HasKey(c => c.Id);

        // decimal(18,2): evita la advertencia de EF por precisión no especificada en columnas monetarias.
        builder.Property(c => c.TarifaMensual).HasPrecision(18, 2);
        builder.Property(c => c.FechaInicio).HasColumnType("date");
        builder.Property(c => c.Activo).HasDefaultValue(true);

        // La relación Cliente (1) -> Contrato (N) ya quedó definida en ClienteConfiguration.
    }
}
