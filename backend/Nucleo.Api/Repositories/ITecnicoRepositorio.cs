using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public interface ITecnicoRepositorio : IRepositorio<Tecnico>
{
    Task<Tecnico?> ObtenerPorEmailAsync(string email, CancellationToken ct = default);
}
