using Nucleo.Api.Domain;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Tests.Domain;

/// <summary>
/// Pruebas de las tablas de transición (funciones puras, sin mocks). Documentan de forma
/// ejecutable qué movimientos permite cada máquina de estados.
/// </summary>
public class EstadoActivoTransicionesTests
{
    [Theory]
    [InlineData(EstadoActivo.Operativo, EstadoActivo.EnReparacion)]
    [InlineData(EstadoActivo.Operativo, EstadoActivo.EnAlmacen)]
    [InlineData(EstadoActivo.Operativo, EstadoActivo.Retirado)]
    [InlineData(EstadoActivo.EnReparacion, EstadoActivo.Operativo)]
    [InlineData(EstadoActivo.EnAlmacen, EstadoActivo.Operativo)]
    public void EsValida_TransicionesPermitidas_DevuelveTrue(EstadoActivo actual, EstadoActivo nuevo)
        => Assert.True(EstadoActivoTransiciones.EsValida(actual, nuevo));

    [Theory]
    [InlineData(EstadoActivo.Retirado, EstadoActivo.Operativo)]     // terminal
    [InlineData(EstadoActivo.Retirado, EstadoActivo.EnReparacion)]  // terminal
    [InlineData(EstadoActivo.Operativo, EstadoActivo.Operativo)]    // mismo estado
    public void EsValida_TransicionesProhibidas_DevuelveFalse(EstadoActivo actual, EstadoActivo nuevo)
        => Assert.False(EstadoActivoTransiciones.EsValida(actual, nuevo));

    [Fact]
    public void Retirado_EsTerminal_SinTransicionesDeSalida()
        => Assert.Empty(EstadoActivoTransiciones.TransicionesDesde(EstadoActivo.Retirado));
}

public class EstadoTicketTransicionesTests
{
    [Theory]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.EnProgreso)]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Cancelado)]   // escape hatch
    [InlineData(EstadoTicket.EnProgreso, EstadoTicket.Resuelto)]
    [InlineData(EstadoTicket.Resuelto, EstadoTicket.Cerrado)]
    public void EsValida_FlujoNormalYCancelacion_DevuelveTrue(EstadoTicket actual, EstadoTicket nuevo)
        => Assert.True(EstadoTicketTransiciones.EsValida(actual, nuevo));

    [Theory]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Resuelto)]      // no saltar pasos
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Cerrado)]       // no saltar pasos
    [InlineData(EstadoTicket.EnProgreso, EstadoTicket.Cancelado)]  // solo Abierto cancela
    [InlineData(EstadoTicket.Resuelto, EstadoTicket.Abierto)]      // no reabrir
    [InlineData(EstadoTicket.Cerrado, EstadoTicket.Abierto)]       // terminal
    [InlineData(EstadoTicket.Cancelado, EstadoTicket.EnProgreso)]  // terminal
    public void EsValida_TransicionesProhibidas_DevuelveFalse(EstadoTicket actual, EstadoTicket nuevo)
        => Assert.False(EstadoTicketTransiciones.EsValida(actual, nuevo));

    [Theory]
    [InlineData(EstadoTicket.Cerrado)]
    [InlineData(EstadoTicket.Cancelado)]
    public void EstadosTerminales_SinTransicionesDeSalida(EstadoTicket terminal)
        => Assert.Empty(EstadoTicketTransiciones.TransicionesDesde(terminal));
}
