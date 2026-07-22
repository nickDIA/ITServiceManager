using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Services;

public interface ITicketService
{
    /// <summary>
    /// Página de tickets (tamano acotado a [1,100]) con los filtros opcionales.
    /// TotalRegistros es el total del filtro, no de la página: el kanban lo usa como
    /// contador por columna sin traerse todos los tickets.
    /// </summary>
    Task<ResultadoPaginadoDto<TicketResponseDto>> ObtenerTodosAsync(
        int? clienteId, int? tecnicoId, EstadoTicket? estado, int pagina, int tamano, CancellationToken ct = default);

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
