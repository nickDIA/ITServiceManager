using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Services;

public interface ITicketService
{
    Task<IReadOnlyList<TicketResponseDto>> ObtenerTodosAsync(
        int? clienteId, int? tecnicoId, EstadoTicket? estado, CancellationToken ct = default);

    Task<TicketResponseDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<TicketResponseDto> CrearAsync(CrearTicketDto dto, CancellationToken ct = default);

    Task ActualizarAsync(int id, ActualizarTicketDto dto, CancellationToken ct = default);

    Task EliminarAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Cambia el estado de un ticket validando la máquina de estados. A diferencia de
    /// Activo, no hay tabla de auditoría, así que es un único UPDATE (sin transacción
    /// explícita). Si el nuevo estado es terminal (Cerrado/Cancelado), fija FechaCierre.
    /// </summary>
    Task<TicketResponseDto> CambiarEstadoAsync(int id, CambiarEstadoTicketDto dto, CancellationToken ct = default);
}
