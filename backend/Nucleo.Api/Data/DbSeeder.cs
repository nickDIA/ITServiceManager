using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data;

/// <summary>
/// Siembra datos de prueba en tiempo de ejecución (solo en Development).
/// Cada bloque es idempotente por separado (Clientes y Tecnicos se comprueban
/// individualmente), para que agregar un seed nuevo no dependa de que la BD esté vacía.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        await SeedClientesAsync(context, ct);
        await SeedTecnicosAsync(context, ct);
    }

    private static async Task SeedClientesAsync(AppDbContext context, CancellationToken ct)
    {
        if (await context.Clientes.AnyAsync(ct))
            return;

        var clientes = new List<Cliente>
        {
            new()
            {
                Nombre = "Distribuidora del Norte S.A. de C.V.",
                Rfc = "DNO950101AB1",
                Contacto = "María Hernández",
                Telefono = "664-123-4567",
                Activo = true,
                Activos = new List<Activo>
                {
                    new() { Tipo = TipoActivo.Hardware, Nombre = "Servidor Dell PowerEdge R740", NumeroSerie = "SN-DELL-0001", Estado = EstadoActivo.Operativo, FechaAdquisicion = new DateTime(2023, 3, 15) },
                    new() { Tipo = TipoActivo.EquipoRed, Nombre = "Switch Cisco Catalyst 2960", NumeroSerie = "SN-CISCO-0002", Estado = EstadoActivo.Operativo, FechaAdquisicion = new DateTime(2023, 3, 15) }
                }
            },
            new()
            {
                Nombre = "Despacho Contable Robles y Asociados",
                Rfc = "DCR880715H29",
                Contacto = "Jorge Robles",
                Telefono = "664-987-6543",
                Activo = true,
                Activos = new List<Activo>
                {
                    new() { Tipo = TipoActivo.Hardware, Nombre = "Laptop HP EliteBook 840", NumeroSerie = "SN-HP-0003", Estado = EstadoActivo.Operativo, FechaAdquisicion = new DateTime(2024, 1, 10) },
                    new() { Tipo = TipoActivo.Software, Nombre = "Licencia CONTPAQi Contabilidad", NumeroSerie = "SN-CONTPAQ-0004", Estado = EstadoActivo.Operativo, FechaAdquisicion = new DateTime(2024, 1, 12) },
                    new() { Tipo = TipoActivo.Hardware, Nombre = "Impresora multifuncional Epson L5590", NumeroSerie = "SN-EPSON-0005", Estado = EstadoActivo.EnReparacion, FechaAdquisicion = new DateTime(2022, 11, 5) }
                }
            },
            new()
            {
                Nombre = "Clínica Dental Sonrisa Plena",
                Rfc = "CDS101230MY4",
                Contacto = "Dra. Ana Gutiérrez",
                Telefono = "664-555-0199",
                Activo = true,
                Activos = new List<Activo>
                {
                    new() { Tipo = TipoActivo.Hardware, Nombre = "PC recepción HP Pavilion", NumeroSerie = "SN-HP-0006", Estado = EstadoActivo.Operativo, FechaAdquisicion = new DateTime(2023, 8, 20) },
                    new() { Tipo = TipoActivo.EquipoRed, Nombre = "Router Mikrotik hEX S", NumeroSerie = "SN-MKT-0007", Estado = EstadoActivo.EnAlmacen, FechaAdquisicion = new DateTime(2023, 8, 20) }
                }
            }
        };

        await context.Clientes.AddRangeAsync(clientes, ct);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTecnicosAsync(AppDbContext context, CancellationToken ct)
    {
        if (await context.Tecnicos.AnyAsync(ct))
            return;

        // PasswordHash real (BCrypt) se genera en la Fase 3, cuando se implemente login.
        var tecnicos = new List<Tecnico>
        {
            new() { Nombre = "Carlos Méndez", Email = "carlos.mendez@nucleo.mx", PasswordHash = "PENDIENTE_FASE_AUTH", Rol = RolTecnico.Tecnico },
            new() { Nombre = "Sofía Ramírez", Email = "sofia.ramirez@nucleo.mx", PasswordHash = "PENDIENTE_FASE_AUTH", Rol = RolTecnico.Supervisor },
            new() { Nombre = "Diego Torres", Email = "diego.torres@nucleo.mx", PasswordHash = "PENDIENTE_FASE_AUTH", Rol = RolTecnico.Administrador }
        };

        await context.Tecnicos.AddRangeAsync(tecnicos, ct);
        await context.SaveChangesAsync(ct);
    }
}
