using Nucleo.Api.Models.DTOs;

namespace Nucleo.Api.Services;

/// <summary>Lectura de técnicos (para poblar selectores de asignación en el frontend).</summary>
public interface ITecnicoService
{
    Task<IReadOnlyList<TecnicoResponseDto>> ObtenerTodosAsync(CancellationToken ct = default);
}
