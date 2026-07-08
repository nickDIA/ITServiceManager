using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class HistorialActivoRepositorio : Repositorio<HistorialActivo>, IHistorialActivoRepositorio
{
    public HistorialActivoRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<HistorialActivo>> ObtenerPorActivoIdAsync(int activoId, CancellationToken ct = default)
        => await _dbSet
            .Include(h => h.Tecnico)
            .AsNoTracking()
            .Where(h => h.ActivoId == activoId)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync(ct);
}
