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
    /// <summary>Contraseña demo compartida por los 3 técnicos sembrados (solo para pruebas locales).</summary>
    public const string PasswordDemoTecnicos = "Nucleo123!";

    public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        await SeedClientesAsync(context, ct);
        await SeedTecnicosAsync(context, ct);
        await CorregirPasswordsPendientesAsync(context, ct);
        await SeedContratosAsync(context, ct);
        await SeedTicketsAsync(context, ct);
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

        var hash = BCrypt.Net.BCrypt.HashPassword(PasswordDemoTecnicos);
        var tecnicos = new List<Tecnico>
        {
            new() { Nombre = "Carlos Méndez", Email = "carlos.mendez@nucleo.mx", PasswordHash = hash, Rol = RolTecnico.Tecnico },
            new() { Nombre = "Sofía Ramírez", Email = "sofia.ramirez@nucleo.mx", PasswordHash = hash, Rol = RolTecnico.Lector },
            new() { Nombre = "Diego Torres", Email = "diego.torres@nucleo.mx", PasswordHash = hash, Rol = RolTecnico.Admin }
        };

        await context.Tecnicos.AddRangeAsync(tecnicos, ct);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Red de seguridad idempotente: si algún técnico quedó con el placeholder de una fase
    /// anterior a la implementación de auth, le asigna un hash BCrypt real. No-op una vez
    /// aplicado (ningún técnico volverá a tener ese placeholder).
    /// </summary>
    private static async Task CorregirPasswordsPendientesAsync(AppDbContext context, CancellationToken ct)
    {
        var pendientes = await context.Tecnicos
            .Where(t => t.PasswordHash == "PENDIENTE_FASE_AUTH")
            .ToListAsync(ct);

        if (pendientes.Count == 0)
            return;

        var hash = BCrypt.Net.BCrypt.HashPassword(PasswordDemoTecnicos);
        foreach (var tecnico in pendientes)
            tecnico.PasswordHash = hash;

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedContratosAsync(AppDbContext context, CancellationToken ct)
    {
        if (await context.Contratos.AnyAsync(ct))
            return;

        var clientes = await context.Clientes.ToListAsync(ct);

        var contratos = clientes.Select(cliente => new Contrato
        {
            ClienteId = cliente.Id,
            // Referencia por Rfc (no por Id) porque el Id depende del historial de la BD.
            TarifaMensual = cliente.Rfc switch
            {
                "DNO950101AB1" => 8000m,
                "DCR880715H29" => 5000m,
                "CDS101230MY4" => 6000m,
                _ => 4000m
            },
            HorasIncluidas = cliente.Rfc switch
            {
                "DNO950101AB1" => 20,
                "DCR880715H29" => 15,
                "CDS101230MY4" => 10,
                _ => 10
            },
            SlaHoras = cliente.Rfc == "DNO950101AB1" ? 4 : 8,
            FechaInicio = new DateTime(2023, 1, 1),
            Activo = true
        }).ToList();

        await context.Contratos.AddRangeAsync(contratos, ct);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTicketsAsync(AppDbContext context, CancellationToken ct)
    {
        if (await context.Tickets.AnyAsync(ct))
            return;

        var clientes = await context.Clientes.Include(c => c.Activos).ToListAsync(ct);
        var tecnicos = await context.Tecnicos.ToListAsync(ct);

        var carlos = tecnicos.First(t => t.Email == "carlos.mendez@nucleo.mx");
        var diego = tecnicos.First(t => t.Email == "diego.torres@nucleo.mx");

        var cliente1 = clientes.First(c => c.Rfc == "DNO950101AB1");
        var cliente2 = clientes.First(c => c.Rfc == "DCR880715H29");
        var cliente3 = clientes.First(c => c.Rfc == "CDS101230MY4");
        var impresora = cliente2.Activos.First(a => a.NumeroSerie == "SN-EPSON-0005");

        // cliente3 queda sin tickets Abierto/EnProgreso a propósito: alimenta la
        // subconsulta "clientes sin tickets abiertos" del dashboard con un caso real.
        var tickets = new List<Ticket>
        {
            new()
            {
                ClienteId = cliente1.Id, ActivoId = null,
                Titulo = "Servidor no responde", Descripcion = "El servidor principal dejó de responder esta mañana.",
                Prioridad = Prioridad.Critica, Estado = EstadoTicket.Abierto, TecnicoId = carlos.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                ClienteId = cliente2.Id, ActivoId = null,
                Titulo = "Solicitud de nueva licencia", Descripcion = "Requieren una licencia adicional de CONTPAQi.",
                Prioridad = Prioridad.Baja, Estado = EstadoTicket.Abierto, TecnicoId = diego.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                ClienteId = cliente2.Id, ActivoId = impresora.Id,
                Titulo = "Impresora atascada", Descripcion = "La impresora multifuncional presenta atascos frecuentes.",
                Prioridad = Prioridad.Media, Estado = EstadoTicket.EnProgreso, TecnicoId = carlos.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                ClienteId = cliente3.Id, ActivoId = null,
                Titulo = "Configurar VPN para personal remoto", Descripcion = "Solicitan acceso remoto seguro para dos doctoras.",
                Prioridad = Prioridad.Alta, Estado = EstadoTicket.Resuelto, TecnicoId = diego.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                ClienteId = cliente1.Id, ActivoId = null,
                Titulo = "Capacitación de Office", Descripcion = "Capacitación básica de Excel para el equipo administrativo.",
                Prioridad = Prioridad.Baja, Estado = EstadoTicket.Cerrado, TecnicoId = carlos.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-20), FechaCierre = DateTime.UtcNow.AddDays(-18)
            },
            new()
            {
                ClienteId = cliente3.Id, ActivoId = null,
                Titulo = "Migración de correo cancelada por el cliente", Descripcion = "El cliente decidió posponer la migración indefinidamente.",
                Prioridad = Prioridad.Media, Estado = EstadoTicket.Cancelado, TecnicoId = diego.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-15), FechaCierre = DateTime.UtcNow.AddDays(-14)
            }
        };

        await context.Tickets.AddRangeAsync(tickets, ct);
        await context.SaveChangesAsync(ct);
    }
}
