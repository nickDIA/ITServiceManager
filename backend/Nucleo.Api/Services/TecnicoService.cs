using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Repositories;

namespace Nucleo.Api.Services;

public class TecnicoService : ITecnicoService
{
    private readonly ITecnicoRepositorio _repositorio;

    public TecnicoService(ITecnicoRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IReadOnlyList<TecnicoResponseDto>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        var tecnicos = await _repositorio.ObtenerTodosAsync(ct);
        return tecnicos
            .OrderBy(t => t.Nombre)
            .Select(t => new TecnicoResponseDto { Id = t.Id, Nombre = t.Nombre, Email = t.Email, Rol = t.Rol })
            .ToList();
    }
}
