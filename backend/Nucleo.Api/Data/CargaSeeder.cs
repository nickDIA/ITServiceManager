using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Data;

/// <summary>
/// Sembrador de VOLUMEN para pruebas de rendimiento — separado de <see cref="DbSeeder"/>
/// (que es data demo mínima). Gateado: solo corre con
/// <c>dotnet run --project backend/Nucleo.Api -- seed-bulk &lt;realista|estres&gt;</c>,
/// nunca al arrancar el host web ni en producción.
///
/// Idempotente por "top-up": lleva cada tabla hasta el objetivo del nivel; re-ejecutar el
/// mismo nivel no agrega nada, y subir de nivel (realista → estres) solo agrega la diferencia.
/// Los datos de carga usan prefijos propios (RFC <c>LC…</c>, serie <c>SN-LC-…</c>, email
/// <c>carga…@carga.test</c>) para no chocar con el seed demo y poder identificarlos/limpiarlos.
///
/// Técnica clave de EF a escala: <c>AutoDetectChangesEnabled = false</c> + insertar en lotes
/// con <c>ChangeTracker.Clear()</c> entre lotes. Sin esto, el change tracker crece sin límite
/// y las inserciones se degradan a O(n²).
/// </summary>
public static class CargaSeeder
{
    public enum Nivel { Realista, Estres }

    private sealed record Objetivo(int Clientes, int ActivosPorCliente, int Tickets, int Tecnicos);

    private static Objetivo Para(Nivel n) => n switch
    {
        // ~800 activos, 3k tickets: un MSP real y maduro.
        Nivel.Realista => new Objetivo(Clientes: 50, ActivosPorCliente: 16, Tickets: 3_000, Tecnicos: 20),
        // ~50k activos, 200k tickets: fuerza a que salten índices faltantes y endpoints O(n).
        Nivel.Estres => new Objetivo(Clientes: 1_000, ActivosPorCliente: 50, Tickets: 200_000, Tecnicos: 50),
        _ => throw new ArgumentOutOfRangeException(nameof(n))
    };

    private const int LoteClientes = 2_000;
    private const int LoteActivos = 5_000;
    private const int LoteTickets = 5_000;

    public static async Task SeedAsync(AppDbContext context, string nivelArg, CancellationToken ct = default)
    {
        var nivel = ParseNivel(nivelArg);
        var obj = Para(nivel);
        var rng = new Random(20260721); // semilla fija: generación reproducible
        var sw = Stopwatch.StartNew();

        Log($"Nivel {nivel}: objetivo {obj.Clientes:N0} clientes · ~{obj.Clientes * obj.ActivosPorCliente:N0} activos · {obj.Tickets:N0} tickets · {obj.Tecnicos} técnicos.");
        Log("Nota: es un comando de mantenimiento local, no arranca el host web.");

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            await SembrarTecnicosAsync(context, obj, rng, ct);
            await SembrarClientesAsync(context, obj, rng, ct);

            var clienteIds = await context.Clientes.Select(c => c.Id).ToListAsync(ct);
            var tecnicoIds = await context.Tecnicos.Select(t => t.Id).ToListAsync(ct);

            await SembrarActivosAsync(context, obj, clienteIds, rng, ct);

            // Mapa cliente -> sus activos, para que los tickets con activo respeten la regla
            // de negocio (el activo debe pertenecer al cliente del ticket).
            var activosPorCliente = (await context.Activos
                    .Select(a => new { a.Id, a.ClienteId })
                    .ToListAsync(ct))
                .GroupBy(a => a.ClienteId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Id).ToList());

            await SembrarTicketsAsync(context, obj, clienteIds, activosPorCliente, tecnicoIds, rng, ct);
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        Log($"Listo en {sw.Elapsed.TotalSeconds:F1}s.");
    }

    // ------------------------------------------------------------ Técnicos

    private static async Task SembrarTecnicosAsync(AppDbContext ctx, Objetivo obj, Random rng, CancellationToken ct)
    {
        var total = await ctx.Tecnicos.CountAsync(ct);
        var yaDeCarga = await ctx.Tecnicos.CountAsync(t => t.Email.StartsWith("carga"), ct);
        var faltan = Math.Max(0, obj.Tecnicos - total);
        if (faltan == 0) { Log("Técnicos: ya en objetivo."); return; }

        // Un solo hash BCrypt (caro) compartido: para carga no necesitamos claves distintas.
        var hash = BCrypt.Net.BCrypt.HashPassword(DbSeeder.PasswordDemoTecnicos);
        await InsertarEnLotesAsync(ctx, GenerarTecnicos(yaDeCarga, faltan, hash, rng), LoteClientes, ct, "Técnicos");
    }

    private static IEnumerable<Tecnico> GenerarTecnicos(int desde, int cuantos, string hash, Random rng)
    {
        for (var k = 0; k < cuantos; k++)
        {
            var i = desde + k;
            yield return new Tecnico
            {
                Nombre = $"Técnico Carga {i:D4}",
                Email = $"carga{i}@carga.test",
                PasswordHash = hash,
                Rol = Ponderado(rng, (RolTecnico.Tecnico, 70), (RolTecnico.Lector, 20), (RolTecnico.Admin, 10))
            };
        }
    }

    // ------------------------------------------------------------ Clientes (+ 1 contrato c/u)

    private static async Task SembrarClientesAsync(AppDbContext ctx, Objetivo obj, Random rng, CancellationToken ct)
    {
        var total = await ctx.Clientes.CountAsync(ct);
        var yaDeCarga = await ctx.Clientes.CountAsync(c => c.Rfc.StartsWith("LC"), ct);
        var faltan = Math.Max(0, obj.Clientes - total);
        if (faltan == 0) { Log("Clientes: ya en objetivo."); return; }

        await InsertarEnLotesAsync(ctx, GenerarClientes(yaDeCarga, faltan, rng), LoteClientes, ct, "Clientes");
    }

    private static IEnumerable<Cliente> GenerarClientes(int desde, int cuantos, Random rng)
    {
        for (var k = 0; k < cuantos; k++)
        {
            var i = desde + k;
            yield return new Cliente
            {
                Nombre = $"Empresa Carga {i:D6} S.A. de C.V.",
                Rfc = $"LC{i:D9}",                       // 2+9 = 11 <= 13; único por i; no choca con RFCs demo
                Contacto = $"Contacto {i:D6}",
                Telefono = $"664-{rng.Next(100, 1000)}-{rng.Next(1000, 10000)}",
                Activo = rng.Next(100) < 90,             // ~10% inactivos (alimenta la métrica "clientes activos")
                // Un contrato por cliente como navegación: EF lo inserta junto con el cliente.
                Contratos = new List<Contrato>
                {
                    new()
                    {
                        TarifaMensual = rng.Next(3, 16) * 1000m,
                        HorasIncluidas = rng.Next(5, 41),
                        SlaHoras = Ponderado(rng, (4, 25), (8, 40), (24, 25), (48, 10)),
                        FechaInicio = DateTime.UtcNow.Date.AddDays(-rng.Next(30, 3 * 365)),
                        Activo = rng.Next(100) < 90      // ~10% de contratos inactivos
                    }
                }
            };
        }
    }

    // ------------------------------------------------------------ Activos

    private static async Task SembrarActivosAsync(AppDbContext ctx, Objetivo obj, IReadOnlyList<int> clienteIds, Random rng, CancellationToken ct)
    {
        var objetivo = obj.Clientes * obj.ActivosPorCliente;
        var total = await ctx.Activos.CountAsync(ct);
        var yaDeCarga = await ctx.Activos.CountAsync(a => a.NumeroSerie.StartsWith("SN-LC-"), ct);
        var faltan = Math.Max(0, objetivo - total);
        if (faltan == 0) { Log("Activos: ya en objetivo."); return; }

        await InsertarEnLotesAsync(ctx, GenerarActivos(yaDeCarga, faltan, clienteIds, rng), LoteActivos, ct, "Activos");
    }

    private static IEnumerable<Activo> GenerarActivos(int desde, int cuantos, IReadOnlyList<int> clienteIds, Random rng)
    {
        for (var k = 0; k < cuantos; k++)
        {
            var i = desde + k;
            var tipo = Ponderado(rng, (TipoActivo.Hardware, 50), (TipoActivo.Software, 30), (TipoActivo.EquipoRed, 20));
            yield return new Activo
            {
                ClienteId = clienteIds[rng.Next(clienteIds.Count)],
                Tipo = tipo,
                Nombre = $"{tipo} #{i:D6}",
                NumeroSerie = $"SN-LC-{i:D9}",           // único por i; no choca con series demo
                Estado = Ponderado(rng,
                    (EstadoActivo.Operativo, 70), (EstadoActivo.EnAlmacen, 15),
                    (EstadoActivo.EnReparacion, 10), (EstadoActivo.Retirado, 5)),
                FechaAdquisicion = DateTime.UtcNow.Date.AddDays(-rng.Next(30, 5 * 365))
            };
        }
    }

    // ------------------------------------------------------------ Tickets

    private static async Task SembrarTicketsAsync(
        AppDbContext ctx, Objetivo obj, IReadOnlyList<int> clienteIds,
        Dictionary<int, List<int>> activosPorCliente, IReadOnlyList<int> tecnicoIds, Random rng, CancellationToken ct)
    {
        var total = await ctx.Tickets.CountAsync(ct);
        var faltan = Math.Max(0, obj.Tickets - total);
        if (faltan == 0) { Log("Tickets: ya en objetivo."); return; }

        await InsertarEnLotesAsync(ctx,
            GenerarTickets(faltan, clienteIds, activosPorCliente, tecnicoIds, rng),
            LoteTickets, ct, "Tickets");
    }

    private static IEnumerable<Ticket> GenerarTickets(
        int cuantos, IReadOnlyList<int> clienteIds, Dictionary<int, List<int>> activosPorCliente,
        IReadOnlyList<int> tecnicoIds, Random rng)
    {
        var ahora = DateTime.UtcNow;
        for (var k = 0; k < cuantos; k++)
        {
            var clienteId = clienteIds[rng.Next(clienteIds.Count)];

            // ~60% de los tickets referencian un activo del MISMO cliente (regla de negocio).
            int? activoId = null;
            if (rng.Next(100) < 60 && activosPorCliente.TryGetValue(clienteId, out var activos) && activos.Count > 0)
                activoId = activos[rng.Next(activos.Count)];

            var estado = Ponderado(rng,
                (EstadoTicket.Abierto, 20), (EstadoTicket.EnProgreso, 15),
                (EstadoTicket.Resuelto, 20), (EstadoTicket.Cerrado, 35), (EstadoTicket.Cancelado, 10));

            // Repartido en el último año. Los Abierto/EnProgreso viejos + SLA de horas hacen
            // que muchos salgan "incumplido" en el badge del kanban: caso real para el front.
            var creacion = ahora.AddDays(-rng.Next(1, 365)).AddHours(-rng.Next(0, 24));

            DateTime? cierre = null;
            if (estado is EstadoTicket.Cerrado or EstadoTicket.Cancelado)
            {
                cierre = creacion.AddDays(rng.Next(1, 30));
                if (cierre > ahora) cierre = ahora;
            }

            yield return new Ticket
            {
                ClienteId = clienteId,
                ActivoId = activoId,
                Titulo = $"Incidencia de carga #{k:D6}",
                Descripcion = "Ticket generado para pruebas de rendimiento (datos de carga).",
                Prioridad = Ponderado(rng,
                    (Prioridad.Baja, 30), (Prioridad.Media, 40), (Prioridad.Alta, 20), (Prioridad.Critica, 10)),
                Estado = estado,
                TecnicoId = tecnicoIds[rng.Next(tecnicoIds.Count)],
                FechaCreacion = creacion,
                FechaCierre = cierre
            };
        }
    }

    // ------------------------------------------------------------ Infraestructura

    /// <summary>
    /// Inserta un flujo perezoso de entidades en lotes: AddRange + SaveChanges + Clear por lote.
    /// El Clear() es lo que mantiene el rendimiento constante en vez de degradarse con el volumen.
    /// </summary>
    private static async Task InsertarEnLotesAsync<T>(
        AppDbContext ctx, IEnumerable<T> items, int loteTam, CancellationToken ct, string etiqueta) where T : class
    {
        var buffer = new List<T>(loteTam);
        var total = 0;

        async Task VolcarAsync()
        {
            ctx.Set<T>().AddRange(buffer);
            await ctx.SaveChangesAsync(ct);
            ctx.ChangeTracker.Clear();
            total += buffer.Count;
            buffer.Clear();
            Log($"  {etiqueta}: {total:N0} insertados…");
        }

        foreach (var item in items)
        {
            buffer.Add(item);
            if (buffer.Count >= loteTam)
                await VolcarAsync();
        }
        if (buffer.Count > 0)
            await VolcarAsync();
    }

    private static T Ponderado<T>(Random rng, params (T val, int peso)[] opciones)
    {
        var total = opciones.Sum(o => o.peso);
        var r = rng.Next(total);
        var acc = 0;
        foreach (var (val, peso) in opciones)
        {
            acc += peso;
            if (r < acc) return val;
        }
        return opciones[^1].val;
    }

    private static Nivel ParseNivel(string arg) => arg?.Trim().ToLowerInvariant() switch
    {
        "realista" or "real" or "1x" => Nivel.Realista,
        "estres" or "estrés" or "stress" or "100x" => Nivel.Estres,
        _ => throw new ArgumentException($"Nivel de carga desconocido: '{arg}'. Usa 'realista' o 'estres'.")
    };

    private static void Log(string msg) => Console.WriteLine($"[carga] {msg}");
}
