using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class TicketRepositorio : Repositorio<Ticket>, ITicketRepositorio
{
    public TicketRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Ticket> Items, int Total)> ObtenerPaginadoConJoinsAsync(
        int pagina, int tamano, int? clienteId, int? tecnicoId, EstadoTicket? estado, CancellationToken ct = default)
    {
        // El filtro se aplica sobre la query base (sin los Include) para que el COUNT no
        // arrastre los JOINs: contar no necesita traer cliente/activo/técnico.
        var filtrada = _dbSet.AsNoTracking().AsQueryable();

        if (clienteId is not null)
            filtrada = filtrada.Where(t => t.ClienteId == clienteId);

        if (tecnicoId is not null)
            filtrada = filtrada.Where(t => t.TecnicoId == tecnicoId);

        if (estado is not null)
            filtrada = filtrada.Where(t => t.Estado == estado);

        var total = await filtrada.CountAsync(ct);

        var items = await filtrada
            .Include(t => t.Cliente).ThenInclude(c => c.Contratos)
            .Include(t => t.Activo)
            .Include(t => t.Tecnico)
            .OrderByDescending(t => t.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Ticket?> ObtenerPorIdConJoinsAsync(int id, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Cliente).ThenInclude(c => c.Contratos)
            .Include(t => t.Activo)
            .Include(t => t.Tecnico)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}
