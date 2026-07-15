using Microsoft.EntityFrameworkCore;
using Moq;
using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;
using Nucleo.Api.Tests.Helpers;

namespace Nucleo.Api.Tests.Services;

/// <summary>
/// Pruebas de ActivoService. Las de CambiarEstadoAsync son las centrales del proyecto:
/// verifican que la máquina de estados se consulta ANTES de abrir transacción, el orden
/// Iniciar → guardar activo → guardar historial → Confirmar, y el rollback cuando la
/// escritura de auditoría falla (requisito 6 de la spec).
/// </summary>
public class ActivoServiceTests
{
    private readonly Mock<IActivoRepositorio> _activoRepo = new();
    private readonly Mock<IHistorialActivoRepositorio> _historialRepo = new();
    private readonly Mock<IRepositorio<Cliente>> _clienteRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ActivoService _service;

    public ActivoServiceTests()
    {
        _service = new ActivoService(_activoRepo.Object, _historialRepo.Object, _clienteRepo.Object, _unitOfWork.Object);
    }

    private static Activo ActivoDemo(int id = 1, EstadoActivo estado = EstadoActivo.Operativo) => new()
    {
        Id = id,
        ClienteId = 1,
        Tipo = TipoActivo.Hardware,
        Nombre = "Servidor Dell",
        NumeroSerie = "SN-0001",
        Estado = estado,
        FechaAdquisicion = new DateTime(2023, 3, 15)
    };

    private static CambiarEstadoActivoDto CambioDemo(EstadoActivo nuevo = EstadoActivo.EnReparacion) => new()
    {
        NuevoEstado = nuevo,
        Motivo = "Falla detectada en pruebas"
    };

    // ------------------------------------------------------------ Crear

    [Fact]
    public async Task CrearAsync_ClienteInexistente_LanzaRecursoNoEncontrado()
    {
        _clienteRepo.Setup(r => r.ObtenerPorIdAsync(99, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Cliente?)null);

        var dto = new CrearActivoDto { ClienteId = 99, Tipo = TipoActivo.Hardware, Nombre = "X", NumeroSerie = "SN-X", FechaAdquisicion = DateTime.Today };

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task CrearAsync_NumeroSerieDuplicado_LanzaConflicto()
    {
        _clienteRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Cliente { Id = 1, Nombre = "Cliente" });
        _activoRepo.Setup(r => r.ExisteNumeroSerieAsync("SN-0001", null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        var dto = new CrearActivoDto { ClienteId = 1, Tipo = TipoActivo.Hardware, Nombre = "X", NumeroSerie = "SN-0001", FechaAdquisicion = DateTime.Today };

        await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(dto));
        _activoRepo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_Valido_NaceEnEstadoOperativo()
    {
        _clienteRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Cliente { Id = 1, Nombre = "Cliente" });
        _activoRepo.Setup(r => r.ExisteNumeroSerieAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);

        var dto = new CrearActivoDto { ClienteId = 1, Tipo = TipoActivo.Software, Nombre = "Licencia", NumeroSerie = " SN-NUEVO ", FechaAdquisicion = DateTime.Today };
        var resultado = await _service.CrearAsync(dto);

        Assert.Equal(EstadoActivo.Operativo, resultado.Estado);
        Assert.Equal("SN-NUEVO", resultado.NumeroSerie); // trim aplicado
        _activoRepo.Verify(r => r.AgregarAsync(It.Is<Activo>(a => a.Estado == EstadoActivo.Operativo), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ------------------------------------------------------------ CambiarEstado: validaciones previas

    [Fact]
    public async Task CambiarEstadoAsync_ActivoInexistente_LanzaRecursoNoEncontrado()
    {
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(99, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Activo?)null);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(
            () => _service.CambiarEstadoAsync(99, CambioDemo(), tecnicoId: 4));
    }

    [Theory]
    [InlineData(EstadoActivo.Retirado, EstadoActivo.Operativo)]     // Retirado es terminal
    [InlineData(EstadoActivo.Operativo, EstadoActivo.Operativo)]    // mismo estado no es transición
    public async Task CambiarEstadoAsync_TransicionInvalida_LanzaConflictoSinAbrirTransaccion(
        EstadoActivo actual, EstadoActivo nuevo)
    {
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(ActivoDemo(1, actual));

        await Assert.ThrowsAsync<ConflictoException>(
            () => _service.CambiarEstadoAsync(1, CambioDemo(nuevo), tecnicoId: 4));

        // El diseño exige validar ANTES de abrir la transacción: la BD nunca se toca.
        _unitOfWork.Verify(u => u.IniciarTransaccionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _activoRepo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ------------------------------------------------------------ CambiarEstado: camino feliz

    [Fact]
    public async Task CambiarEstadoAsync_Valido_EjecutaTransaccionCompletaEnOrden()
    {
        var activo = ActivoDemo(1, EstadoActivo.Operativo);
        var orden = new List<string>();

        _activoRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(activo);
        _activoRepo.Setup(r => r.ObtenerPorIdConClienteAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(ActivoDemo(1, EstadoActivo.EnReparacion));

        _unitOfWork.Setup(u => u.IniciarTransaccionAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => orden.Add("iniciar")).Returns(Task.CompletedTask);
        _activoRepo.Setup(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => orden.Add("guardar-activo")).ReturnsAsync(1);
        _historialRepo.Setup(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                      .Callback(() => orden.Add("guardar-historial")).ReturnsAsync(1);
        _unitOfWork.Setup(u => u.ConfirmarTransaccionAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => orden.Add("confirmar")).Returns(Task.CompletedTask);

        HistorialActivo? historialEscrito = null;
        _historialRepo.Setup(r => r.AgregarAsync(It.IsAny<HistorialActivo>(), It.IsAny<CancellationToken>()))
                      .Callback<HistorialActivo, CancellationToken>((h, _) => historialEscrito = h)
                      .Returns(Task.CompletedTask);

        await _service.CambiarEstadoAsync(1, CambioDemo(EstadoActivo.EnReparacion), tecnicoId: 4);

        Assert.Equal(["iniciar", "guardar-activo", "guardar-historial", "confirmar"], orden);
        Assert.Equal(EstadoActivo.EnReparacion, activo.Estado);

        Assert.NotNull(historialEscrito);
        Assert.Equal(EstadoActivo.Operativo, historialEscrito!.EstadoAnterior);
        Assert.Equal(EstadoActivo.EnReparacion, historialEscrito.EstadoNuevo);
        Assert.Equal(4, historialEscrito.TecnicoId); // viene del parámetro (claim JWT), no del DTO

        _unitOfWork.Verify(u => u.RevertirTransaccionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ------------------------------------------------------------ CambiarEstado: rollback (requisito 6)

    [Fact]
    public async Task CambiarEstadoAsync_FallaLaAuditoria_RevierteYPropaga()
    {
        var activo = ActivoDemo(1, EstadoActivo.Operativo);
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(activo);
        _historialRepo.Setup(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new InvalidOperationException("Fallo simulado al escribir auditoría"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CambiarEstadoAsync(1, CambioDemo(), tecnicoId: 4));

        _unitOfWork.Verify(u => u.RevertirTransaccionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ConfirmarTransaccionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_ViolacionFkDeTecnico_TraduceA404YRevierte()
    {
        var activo = ActivoDemo(1, EstadoActivo.Operativo);
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(activo);

        // Simula exactamente lo que hace SQL Server cuando TecnicoId no existe:
        // DbUpdateException envolviendo un SqlException con Number = 547.
        var sqlEx = SqlExceptionFactory.Crear(547);
        _historialRepo.Setup(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new DbUpdateException("FK violation", sqlEx));

        var ex = await Assert.ThrowsAsync<RecursoNoEncontradoException>(
            () => _service.CambiarEstadoAsync(1, CambioDemo(), tecnicoId: 9999));

        Assert.Contains("9999", ex.Message);
        _unitOfWork.Verify(u => u.RevertirTransaccionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ConfirmarTransaccionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_DbUpdateExceptionQueNoEsFk_SePropagaSinTraducir()
    {
        var activo = ActivoDemo(1, EstadoActivo.Operativo);
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(activo);

        // DbUpdateException SIN SqlException 547 adentro: debe ir al catch genérico
        // (rollback + rethrow), no traducirse a 404.
        _historialRepo.Setup(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new DbUpdateException("otro error de BD"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _service.CambiarEstadoAsync(1, CambioDemo(), tecnicoId: 4));

        _unitOfWork.Verify(u => u.RevertirTransaccionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ------------------------------------------------------------ Historial

    [Fact]
    public async Task ObtenerHistorialAsync_ActivoInexistente_LanzaRecursoNoEncontrado()
    {
        _activoRepo.Setup(r => r.ExisteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _service.ObtenerHistorialAsync(99));
    }

    [Fact]
    public async Task ObtenerHistorialAsync_MapeaTecnicoNombre()
    {
        _activoRepo.Setup(r => r.ExisteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historialRepo.Setup(r => r.ObtenerPorActivoIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync([
                          new HistorialActivo
                          {
                              Id = 1, ActivoId = 1,
                              EstadoAnterior = EstadoActivo.Operativo, EstadoNuevo = EstadoActivo.EnReparacion,
                              Motivo = "Falla", Fecha = DateTime.UtcNow, TecnicoId = 4,
                              Tecnico = new Tecnico { Id = 4, Nombre = "Carlos Méndez" }
                          }
                      ]);

        var historial = await _service.ObtenerHistorialAsync(1);

        Assert.Single(historial);
        Assert.Equal("Carlos Méndez", historial[0].TecnicoNombre);
    }
}
