using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

public class ReporteDashboardDto
{
    public int ClientesActivos { get; set; }
    public int TicketsAbiertos { get; set; }
    public int ClientesSinTicketsAbiertos { get; set; }
    public decimal IngresosMensualesRecurrentes { get; set; }
    public double PromedioHorasIncluidasContratos { get; set; }
    public IReadOnlyDictionary<EstadoActivo, int> ActivosPorEstado { get; set; } = new Dictionary<EstadoActivo, int>();
    public IReadOnlyDictionary<EstadoTicket, int> TicketsPorEstado { get; set; } = new Dictionary<EstadoTicket, int>();
    public IReadOnlyDictionary<Prioridad, int> TicketsPorPrioridad { get; set; } = new Dictionary<Prioridad, int>();
}
