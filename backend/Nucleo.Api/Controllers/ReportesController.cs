using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Services;

namespace Nucleo.Api.Controllers;

/// <summary>Solo lectura: cualquier técnico autenticado puede ver métricas.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _service;

    public ReportesController(IReporteService service)
    {
        _service = service;
    }

    /// <summary>
    /// Métricas agregadas: activos por estado, tickets por estado/prioridad (GROUP BY),
    /// ingresos mensuales recurrentes (SUM), promedio de horas incluidas (AVG), y
    /// clientes sin tickets abiertos (subconsulta).
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ReporteDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteDashboardDto>> Dashboard(CancellationToken ct)
        => Ok(await _service.ObtenerDashboardAsync(ct));
}
