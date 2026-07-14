using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public interface ITicketRepositorio : IRepositorio<Ticket>
{
    /// <summary>Lista de tickets con Cliente + Activo + Tecnico cargados (3 JOINs), filtrable.</summary>
    Task<IReadOnlyList<Ticket>> ObtenerTodosConJoinsAsync(
        int? clienteId, int? tecnicoId, EstadoTicket? estado, CancellationToken ct = default);

    Task<Ticket?> ObtenerPorIdConJoinsAsync(int id, CancellationToken ct = default);
}
