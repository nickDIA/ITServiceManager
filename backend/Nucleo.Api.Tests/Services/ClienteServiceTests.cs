using Moq;
using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;

namespace Nucleo.Api.Tests.Services;

/// <summary>
/// Pruebas de ClienteService con el repositorio mockeado: validan reglas de negocio
/// (RFC único, bloqueo de borrado con activos) sin tocar la base de datos.
/// </summary>
public class ClienteServiceTests
{
    private readonly Mock<IClienteRepositorio> _repo = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _service = new ClienteService(_repo.Object);
    }

    private static Cliente ClienteDemo(int id = 1) => new()
    {
        Id = id,
        Nombre = "Distribuidora del Norte",
        Rfc = "DNO950101AB1",
        Contacto = "María",
        Telefono = "664-123-4567",
        Activo = true
    };

    // ------------------------------------------------------------ Lectura

    [Fact]
    public async Task ObtenerTodosAsync_MapeaEntidadesADto()
    {
        _repo.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync([ClienteDemo(1), ClienteDemo(2)]);

        var resultado = await _service.ObtenerTodosAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("DNO950101AB1", resultado[0].Rfc);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_Inexistente_DevuelveNull()
    {
        _repo.Setup(r => r.ObtenerPorIdAsync(99, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Cliente?)null);

        Assert.Null(await _service.ObtenerPorIdAsync(99));
    }

    // ------------------------------------------------------------ Crear

    [Fact]
    public async Task CrearAsync_NormalizaRfcATrimYMayusculas()
    {
        _repo.Setup(r => r.ExisteRfcAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        var dto = new CrearClienteDto { Nombre = "  Nuevo Cliente  ", Rfc = " dno950101ab1 " };
        var resultado = await _service.CrearAsync(dto);

        Assert.Equal("DNO950101AB1", resultado.Rfc);
        Assert.Equal("Nuevo Cliente", resultado.Nombre);
        _repo.Verify(r => r.AgregarAsync(It.Is<Cliente>(c => c.Rfc == "DNO950101AB1" && c.Activo), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_RfcDuplicado_LanzaConflictoYNoGuarda()
    {
        _repo.Setup(r => r.ExisteRfcAsync("DNO950101AB1", null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var dto = new CrearClienteDto { Nombre = "Duplicado", Rfc = "DNO950101AB1" };

        await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ------------------------------------------------------------ Actualizar

    [Fact]
    public async Task ActualizarAsync_Inexistente_LanzaRecursoNoEncontrado()
    {
        _repo.Setup(r => r.ObtenerPorIdAsync(99, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Cliente?)null);

        var dto = new ActualizarClienteDto { Nombre = "X", Rfc = "DNO950101AB1", Activo = true };

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _service.ActualizarAsync(99, dto));
    }

    [Fact]
    public async Task ActualizarAsync_RfcDeOtroCliente_LanzaConflicto()
    {
        _repo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ClienteDemo(1));
        // El RFC ya existe en OTRO registro (excluyendoId = 1).
        _repo.Setup(r => r.ExisteRfcAsync("OTRO010101XX1", 1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var dto = new ActualizarClienteDto { Nombre = "X", Rfc = "OTRO010101XX1", Activo = true };

        await Assert.ThrowsAsync<ConflictoException>(() => _service.ActualizarAsync(1, dto));
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarAsync_Valido_ActualizaCamposYGuarda()
    {
        var cliente = ClienteDemo(1);
        _repo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(cliente);
        _repo.Setup(r => r.ExisteRfcAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        var dto = new ActualizarClienteDto { Nombre = "Nombre Nuevo", Rfc = "DNO950101AB1", Activo = false };
        await _service.ActualizarAsync(1, dto);

        Assert.Equal("Nombre Nuevo", cliente.Nombre);
        Assert.False(cliente.Activo);
        _repo.Verify(r => r.Actualizar(cliente), Times.Once);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ------------------------------------------------------------ Eliminar

    [Fact]
    public async Task EliminarAsync_ConActivosAsociados_LanzaConflictoYNoElimina()
    {
        _repo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ClienteDemo(1));
        _repo.Setup(r => r.TieneActivosAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictoException>(() => _service.EliminarAsync(1));
        _repo.Verify(r => r.Eliminar(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task EliminarAsync_SinActivos_EliminaYGuarda()
    {
        var cliente = ClienteDemo(1);
        _repo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(cliente);
        _repo.Setup(r => r.TieneActivosAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        await _service.EliminarAsync(1);

        _repo.Verify(r => r.Eliminar(cliente), Times.Once);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
