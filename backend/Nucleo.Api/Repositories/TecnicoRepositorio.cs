using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public class TecnicoRepositorio : Repositorio<Tecnico>, ITecnicoRepositorio
{
    public TecnicoRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<Tecnico?> ObtenerPorEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(t => t.Email == email, ct);
}
