using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class ReporteRepositorio : IReporteRepositorio
{
    private readonly AppDbContext _context;

    public ReporteRepositorio(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<EstadoActivo, int>> ContarActivosPorEstadoAsync(CancellationToken ct = default)
        => await _context.Activos
            .GroupBy(a => a.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Cantidad, ct);

    public async Task<IReadOnlyDictionary<EstadoTicket, int>> ContarTicketsPorEstadoAsync(CancellationToken ct = default)
        => await _context.Tickets
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Cantidad, ct);

    public async Task<IReadOnlyDictionary<Prioridad, int>> ContarTicketsPorPrioridadAsync(CancellationToken ct = default)
        => await _context.Tickets
            .GroupBy(t => t.Prioridad)
            .Select(g => new { Prioridad = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Prioridad, x => x.Cantidad, ct);

    public async Task<int> ContarTicketsAbiertosAsync(CancellationToken ct = default)
        => await _context.Tickets.CountAsync(t => t.Estado == EstadoTicket.Abierto || t.Estado == EstadoTicket.EnProgreso, ct);

    public async Task<int> ContarClientesActivosAsync(CancellationToken ct = default)
        => await _context.Clientes.CountAsync(c => c.Activo, ct);

    public async Task<int> ContarClientesSinTicketsAbiertosAsync(CancellationToken ct = default)
        => await _context.Clientes
            .Where(c => c.Activo && !c.Tickets.Any(t => t.Estado == EstadoTicket.Abierto || t.Estado == EstadoTicket.EnProgreso))
            .CountAsync(ct);

    public async Task<decimal> SumarIngresosMensualesRecurrentesAsync(CancellationToken ct = default)
    {
        var contratosActivos = _context.Contratos.Where(c => c.Activo);
        return await contratosActivos.AnyAsync(ct)
            ? await contratosActivos.SumAsync(c => c.TarifaMensual, ct)
            : 0m;
    }

    public async Task<double> PromedioHorasIncluidasContratosActivosAsync(CancellationToken ct = default)
    {
        var contratosActivos = _context.Contratos.Where(c => c.Activo);
        // AverageAsync lanza excepción sobre una secuencia vacía; Sum no, por eso el guard
        // solo hace falta aquí de forma estricta, pero se deja simétrico por claridad.
        return await contratosActivos.AnyAsync(ct)
            ? await contratosActivos.AverageAsync(c => c.HorasIncluidas, ct)
            : 0d;
    }
}
