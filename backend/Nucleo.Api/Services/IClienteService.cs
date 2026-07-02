using Nucleo.Api.Models.DTOs;

namespace Nucleo.Api.Services;

/// <summary>Lógica de negocio de Cliente. Trabaja con DTOs, nunca expone entidades.</summary>
public interface IClienteService
{
    Task<IReadOnlyList<ClienteResponseDto>> ObtenerTodosAsync(CancellationToken ct = default);

    Task<ClienteResponseDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<ClienteResponseDto> CrearAsync(CrearClienteDto dto, CancellationToken ct = default);

    /// <summary>Actualiza el cliente. Lanza si no existe (404) o si el RFC choca con otro (409).</summary>
    Task ActualizarAsync(int id, ActualizarClienteDto dto, CancellationToken ct = default);

    /// <summary>Elimina el cliente. Lanza si no existe (404) o si tiene activos asociados (409).</summary>
    Task EliminarAsync(int id, CancellationToken ct = default);
}
