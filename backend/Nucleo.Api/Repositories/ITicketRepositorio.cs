using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public interface ITicketRepositorio : IRepositorio<Ticket>
{
    /// <summary>
    /// Página de tickets con Cliente + Activo + Tecnico cargados (3 JOINs), filtrable,
    /// más el total que cumple el filtro. El kanban pide una página POR COLUMNA (?estado=),
    /// y usa ese total como contador de la columna.
    /// </summary>
    Task<(IReadOnlyList<Ticket> Items, int Total)> ObtenerPaginadoConJoinsAsync(
        int pagina, int tamano, int? clienteId, int? tecnicoId, EstadoTicket? estado, CancellationToken ct = default);

    Task<Ticket?> ObtenerPorIdConJoinsAsync(int id, CancellationToken ct = default);
}
