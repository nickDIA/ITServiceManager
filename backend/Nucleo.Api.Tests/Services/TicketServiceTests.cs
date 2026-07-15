using Moq;
using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;

namespace Nucleo.Api.Tests.Services;

/// <summary>
/// Pruebas de TicketService: máquina de estados de Ticket, la regla de negocio
/// cruzada (el activo debe pertenecer al cliente del ticket) y el manejo de FechaCierre.
/// </summary>
public class TicketServiceTests
{
    private readonly Mock<ITicketRepositorio> _ticketRepo = new();
    private readonly Mock<IRepositorio<Cliente>> _clienteRepo = new();
    private readonly Mock<IRepositorio<Activo>> _activoRepo = new();
    private readonly Mock<IRepositorio<Tecnico>> _tecnicoRepo = new();
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _service = new TicketService(_ticketRepo.Object, _clienteRepo.Object, _activoRepo.Object, _tecnicoRepo.Object);
    }

    private static Ticket TicketDemo(int id = 1, EstadoTicket estado = EstadoTicket.Abierto) => new()
    {
        Id = id,
        ClienteId = 1,
        Titulo = "Servidor caído",
        Descripcion = "No responde desde la mañana",
        Prioridad = Prioridad.Critica,
        Estado = estado,
        TecnicoId = 4,
        FechaCreacion = DateTime.UtcNow.AddDays(-1),
        Cliente = new Cliente { Id = 1, Nombre = "Cliente Demo" },
        Tecnico = new Tecnico { Id = 4, Nombre = "Carlos Méndez" }
    };

    private static CrearTicketDto CrearDto(int? activoId = null) => new()
    {
        ClienteId = 1,
        ActivoId = activoId,
        Titulo = "Nuevo ticket",
        Descripcion = "Descripción del problema",
        Prioridad = Prioridad.Media,
        TecnicoId = 4
    };

    private void ClienteYTecnicoExisten()
    {
        _clienteRepo.Setup(r => r.ExisteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _tecnicoRepo.Setup(r => r.ExisteAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    // ------------------------------------------------------------ Crear: validaciones de existencia

    [Fact]
    public async Task CrearAsync_ClienteInexistente_LanzaRecursoNoEncontrado()
    {
        _clienteRepo.Setup(r => r.ExisteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _service.CrearAsync(CrearDto()));
    }

    [Fact]
    public async Task CrearAsync_TecnicoInexistente_LanzaRecursoNoEncontrado()
    {
        _clienteRepo.Setup(r => r.ExisteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _tecnicoRepo.Setup(r => r.ExisteAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _service.CrearAsync(CrearDto()));
    }

    // ------------------------------------------------------------ Crear: regla activo-pertenece-al-cliente

    [Fact]
    public async Task CrearAsync_ActivoDeOtroCliente_LanzaConflicto()
    {
        ClienteYTecnicoExisten();
        // El activo 5 existe pero pertenece al cliente 2, no al 1 del ticket.
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Activo { Id = 5, ClienteId = 2 });

        await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(CrearDto(activoId: 5)));
        _ticketRepo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_ActivoInexistente_LanzaRecursoNoEncontrado()
    {
        ClienteYTecnicoExisten();
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(99, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Activo?)null);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _service.CrearAsync(CrearDto(activoId: 99)));
    }

    [Fact]
    public async Task CrearAsync_Valido_NaceAbiertoConFechaCreacion()
    {
        ClienteYTecnicoExisten();
        Ticket? guardado = null;
        _ticketRepo.Setup(r => r.AgregarAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
                   .Callback<Ticket, CancellationToken>((t, _) => guardado = t)
                   .Returns(Task.CompletedTask);
        _ticketRepo.Setup(r => r.ObtenerPorIdConJoinsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(TicketDemo());

        await _service.CrearAsync(CrearDto());

        Assert.NotNull(guardado);
        Assert.Equal(EstadoTicket.Abierto, guardado!.Estado);
        Assert.True(guardado.FechaCreacion > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(guardado.FechaCierre);
    }

    // ------------------------------------------------------------ Máquina de estados

    [Theory]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Resuelto)]      // no se puede saltar EnProgreso
    [InlineData(EstadoTicket.EnProgreso, EstadoTicket.Cancelado)]  // solo Abierto puede cancelar
    [InlineData(EstadoTicket.Cerrado, EstadoTicket.Abierto)]       // Cerrado es terminal
    [InlineData(EstadoTicket.Cancelado, EstadoTicket.Abierto)]     // Cancelado es terminal
    public async Task CambiarEstadoAsync_TransicionInvalida_LanzaConflicto(EstadoTicket actual, EstadoTicket nuevo)
    {
        _ticketRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(TicketDemo(1, actual));

        await Assert.ThrowsAsync<ConflictoException>(
            () => _service.CambiarEstadoAsync(1, new CambiarEstadoTicketDto { NuevoEstado = nuevo }));

        _ticketRepo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_AEstadoNoTerminal_NoFijaFechaCierre()
    {
        var ticket = TicketDemo(1, EstadoTicket.Abierto);
        _ticketRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.ObtenerPorIdConJoinsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await _service.CambiarEstadoAsync(1, new CambiarEstadoTicketDto { NuevoEstado = EstadoTicket.EnProgreso });

        Assert.Equal(EstadoTicket.EnProgreso, ticket.Estado);
        Assert.Null(ticket.FechaCierre);
    }

    [Theory]
    [InlineData(EstadoTicket.Resuelto, EstadoTicket.Cerrado)]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Cancelado)]
    public async Task CambiarEstadoAsync_AEstadoTerminal_FijaFechaCierre(EstadoTicket actual, EstadoTicket terminal)
    {
        var ticket = TicketDemo(1, actual);
        _ticketRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.ObtenerPorIdConJoinsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await _service.CambiarEstadoAsync(1, new CambiarEstadoTicketDto { NuevoEstado = terminal });

        Assert.Equal(terminal, ticket.Estado);
        Assert.NotNull(ticket.FechaCierre);
    }

    // ------------------------------------------------------------ Actualizar

    [Fact]
    public async Task ActualizarAsync_ValidaActivoContraElClienteDelTicket_NoDelDto()
    {
        // El ticket pertenece al cliente 1; se intenta asociar un activo del cliente 2.
        var ticket = TicketDemo(1);
        _ticketRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _tecnicoRepo.Setup(r => r.ExisteAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _activoRepo.Setup(r => r.ObtenerPorIdAsync(7, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Activo { Id = 7, ClienteId = 2 });

        var dto = new ActualizarTicketDto
        {
            ActivoId = 7,
            Titulo = "Actualizado",
            Descripcion = "Descripción nueva",
            Prioridad = Prioridad.Alta,
            TecnicoId = 4
        };

        await Assert.ThrowsAsync<ConflictoException>(() => _service.ActualizarAsync(1, dto));
    }
}
