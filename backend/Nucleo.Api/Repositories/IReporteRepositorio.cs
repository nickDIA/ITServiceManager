using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

/// <summary>
/// Repositorio de agregaciones/reportes: cruza varias tablas (Cliente, Activo, Ticket,
/// Contrato), así que no encaja en ningún repositorio específico de una sola entidad.
/// Sigue tocando EF Core solo aquí, no en el service.
/// </summary>
public interface IReporteRepositorio
{
    Task<IReadOnlyDictionary<EstadoActivo, int>> ContarActivosPorEstadoAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<EstadoTicket, int>> ContarTicketsPorEstadoAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<Prioridad, int>> ContarTicketsPorPrioridadAsync(CancellationToken ct = default);
    Task<int> ContarTicketsAbiertosAsync(CancellationToken ct = default);
    Task<int> ContarClientesActivosAsync(CancellationToken ct = default);

    /// <summary>Subconsulta: clientes activos sin ningún ticket Abierto/EnProgreso.</summary>
    Task<int> ContarClientesSinTicketsAbiertosAsync(CancellationToken ct = default);

    Task<decimal> SumarIngresosMensualesRecurrentesAsync(CancellationToken ct = default);
    Task<double> PromedioHorasIncluidasContratosActivosAsync(CancellationToken ct = default);
}
