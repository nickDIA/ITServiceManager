using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data;

/// <summary>
/// Único punto de acceso a la base de datos. Las capas superiores (servicios) nunca lo tocan
/// directamente: pasan por los repositorios.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<HistorialActivo> HistorialActivos => Set<HistorialActivo>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Tecnico> Tecnicos => Set<Tecnico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Carga todas las clases IEntityTypeConfiguration<T> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
