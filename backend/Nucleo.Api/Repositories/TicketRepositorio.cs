using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class TicketRepositorio : Repositorio<Ticket>, ITicketRepositorio
{
    public TicketRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Ticket>> ObtenerTodosConJoinsAsync(
        int? clienteId, int? tecnicoId, EstadoTicket? estado, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(t => t.Cliente)
            .Include(t => t.Activo)
            .Include(t => t.Tecnico)
            .AsNoTracking()
            .AsQueryable();

        if (clienteId is not null)
            query = query.Where(t => t.ClienteId == clienteId);

        if (tecnicoId is not null)
            query = query.Where(t => t.TecnicoId == tecnicoId);

        if (estado is not null)
            query = query.Where(t => t.Estado == estado);

        return await query.OrderByDescending(t => t.FechaCreacion).ToListAsync(ct);
    }

    public async Task<Ticket?> ObtenerPorIdConJoinsAsync(int id, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Cliente)
            .Include(t => t.Activo)
            .Include(t => t.Tecnico)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}
