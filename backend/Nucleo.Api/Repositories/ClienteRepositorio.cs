using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class ClienteRepositorio : Repositorio<Cliente>, IClienteRepositorio
{
    public ClienteRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> ExisteRfcAsync(string rfc, int? excluyendoId = null, CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .AnyAsync(c => c.Rfc == rfc && (excluyendoId == null || c.Id != excluyendoId), ct);

    public async Task<bool> TieneActivosAsync(int clienteId, CancellationToken ct = default)
        => await _context.Set<Activo>().AnyAsync(a => a.ClienteId == clienteId, ct);

    public async Task<(IReadOnlyList<Cliente> Items, int Total)> ObtenerPaginadoAsync(int pagina, int tamano, CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking().OrderBy(c => c.Nombre);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);

        return (items, total);
    }
}
