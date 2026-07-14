using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Repositories;

namespace Nucleo.Api.Services;

public class ReporteService : IReporteService
{
    private readonly IReporteRepositorio _repositorio;

    public ReporteService(IReporteRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ReporteDashboardDto> ObtenerDashboardAsync(CancellationToken ct = default)
    {
        // Secuencial a propósito, NO Task.WhenAll: todas las llamadas comparten el mismo
        // AppDbContext (Scoped), y EF Core no soporta operaciones concurrentes sobre la
        // misma instancia de DbContext (lanzaría InvalidOperationException en runtime).
        var activosPorEstado = await _repositorio.ContarActivosPorEstadoAsync(ct);
        var ticketsPorEstado = await _repositorio.ContarTicketsPorEstadoAsync(ct);
        var ticketsPorPrioridad = await _repositorio.ContarTicketsPorPrioridadAsync(ct);
        var ticketsAbiertos = await _repositorio.ContarTicketsAbiertosAsync(ct);
        var clientesActivos = await _repositorio.ContarClientesActivosAsync(ct);
        var clientesSinTicketsAbiertos = await _repositorio.ContarClientesSinTicketsAbiertosAsync(ct);
        var ingresosMensuales = await _repositorio.SumarIngresosMensualesRecurrentesAsync(ct);
        var promedioHoras = await _repositorio.PromedioHorasIncluidasContratosActivosAsync(ct);

        return new ReporteDashboardDto
        {
            ClientesActivos = clientesActivos,
            TicketsAbiertos = ticketsAbiertos,
            ClientesSinTicketsAbiertos = clientesSinTicketsAbiertos,
            IngresosMensualesRecurrentes = ingresosMensuales,
            PromedioHorasIncluidasContratos = Math.Round(promedioHoras, 1),
            ActivosPorEstado = activosPorEstado,
            TicketsPorEstado = ticketsPorEstado,
            TicketsPorPrioridad = ticketsPorPrioridad
        };
    }
}
