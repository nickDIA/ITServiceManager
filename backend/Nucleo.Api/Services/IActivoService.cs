using Nucleo.Api.Models.DTOs;

namespace Nucleo.Api.Services;

public interface IActivoService
{
    /// <summary>Página de activos ordenados por nombre, opcionalmente filtrada por cliente. tamano se acota entre 1 y 100.</summary>
    Task<ResultadoPaginadoDto<ActivoResponseDto>> ObtenerTodosAsync(int? clienteId, int pagina, int tamano, CancellationToken ct = default);

    Task<ActivoResponseDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<ActivoResponseDto> CrearAsync(CrearActivoDto dto, CancellationToken ct = default);

    Task ActualizarAsync(int id, ActualizarActivoDto dto, CancellationToken ct = default);

    Task EliminarAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Cambia el estado de un activo. Valida la transición contra la máquina de estados,
    /// y registra el cambio en HistorialActivo dentro de la MISMA transacción: si el
    /// registro de auditoría falla, el cambio de estado se revierte. tecnicoId viene del
    /// claim del JWT (lo resuelve el controller), no del DTO.
    /// </summary>
    Task<ActivoResponseDto> CambiarEstadoAsync(int id, CambiarEstadoActivoDto dto, int tecnicoId, CancellationToken ct = default);

    Task<IReadOnlyList<HistorialActivoResponseDto>> ObtenerHistorialAsync(int activoId, CancellationToken ct = default);
}
