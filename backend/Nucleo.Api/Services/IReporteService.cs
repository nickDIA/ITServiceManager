using Nucleo.Api.Models.DTOs;

namespace Nucleo.Api.Services;

public interface IReporteService
{
    Task<ReporteDashboardDto> ObtenerDashboardAsync(CancellationToken ct = default);
}
