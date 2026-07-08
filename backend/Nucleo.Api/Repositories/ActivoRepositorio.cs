using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class ActivoRepositorio : Repositorio<Activo>, IActivoRepositorio
{
    public ActivoRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Activo>> ObtenerTodosConClienteAsync(int? clienteId, CancellationToken ct = default)
    {
        var query = _dbSet.Include(a => a.Cliente).AsNoTracking().AsQueryable();

        if (clienteId is not null)
            query = query.Where(a => a.ClienteId == clienteId);

        return await query.OrderBy(a => a.Nombre).ToListAsync(ct);
    }

    public async Task<Activo?> ObtenerPorIdConClienteAsync(int id, CancellationToken ct = default)
        => await _dbSet.Include(a => a.Cliente).AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> ExisteNumeroSerieAsync(string numeroSerie, int? excluyendoId = null, CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .AnyAsync(a => a.NumeroSerie == numeroSerie && (excluyendoId == null || a.Id != excluyendoId), ct);
}
