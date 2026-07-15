using Moq;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;

namespace Nucleo.Api.Tests.Services;

/// <summary>
/// Pruebas de ReporteService: el service solo orquesta el repositorio de agregaciones y
/// arma el DTO, así que se verifica el mapeo campo a campo y el redondeo del promedio.
/// </summary>
public class ReporteServiceTests
{
    private readonly Mock<IReporteRepositorio> _repo = new();
    private readonly ReporteService _service;

    public ReporteServiceTests()
    {
        _service = new ReporteService(_repo.Object);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_MapeaTodasLasMetricasDelRepositorio()
    {
        var activosPorEstado = new Dictionary<EstadoActivo, int> { [EstadoActivo.Operativo] = 5, [EstadoActivo.EnReparacion] = 1 };
        var ticketsPorEstado = new Dictionary<EstadoTicket, int> { [EstadoTicket.Abierto] = 2, [EstadoTicket.Cerrado] = 1 };
        var ticketsPorPrioridad = new Dictionary<Prioridad, int> { [Prioridad.Critica] = 1, [Prioridad.Baja] = 2 };

        _repo.Setup(r => r.ContarActivosPorEstadoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(activosPorEstado);
        _repo.Setup(r => r.ContarTicketsPorEstadoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ticketsPorEstado);
        _repo.Setup(r => r.ContarTicketsPorPrioridadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ticketsPorPrioridad);
        _repo.Setup(r => r.ContarTicketsAbiertosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _repo.Setup(r => r.ContarClientesActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _repo.Setup(r => r.ContarClientesSinTicketsAbiertosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.SumarIngresosMensualesRecurrentesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(19000m);
        _repo.Setup(r => r.PromedioHorasIncluidasContratosActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15.0);

        var dashboard = await _service.ObtenerDashboardAsync();

        Assert.Equal(3, dashboard.ClientesActivos);
        Assert.Equal(3, dashboard.TicketsAbiertos);
        Assert.Equal(1, dashboard.ClientesSinTicketsAbiertos);
        Assert.Equal(19000m, dashboard.IngresosMensualesRecurrentes);
        Assert.Equal(15.0, dashboard.PromedioHorasIncluidasContratos);
        Assert.Equal(activosPorEstado, dashboard.ActivosPorEstado);
        Assert.Equal(ticketsPorEstado, dashboard.TicketsPorEstado);
        Assert.Equal(ticketsPorPrioridad, dashboard.TicketsPorPrioridad);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_RedondeaElPromedioAUnDecimal()
    {
        _repo.Setup(r => r.ContarActivosPorEstadoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<EstadoActivo, int>());
        _repo.Setup(r => r.ContarTicketsPorEstadoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<EstadoTicket, int>());
        _repo.Setup(r => r.ContarTicketsPorPrioridadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Prioridad, int>());
        _repo.Setup(r => r.PromedioHorasIncluidasContratosActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(16.666666);

        var dashboard = await _service.ObtenerDashboardAsync();

        Assert.Equal(16.7, dashboard.PromedioHorasIncluidasContratos);
    }
}
