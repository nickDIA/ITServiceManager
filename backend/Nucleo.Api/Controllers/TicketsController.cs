using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleo.Api.Common;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Services;

namespace Nucleo.Api.Controllers;

/// <summary>Requiere token (cualquier rol puede leer; escritura restringida a Admin/Tecnico).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _service;

    public TicketsController(ITicketService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista tickets paginados, con cliente + activo + técnico ya resueltos.
    /// Filtra con ?clienteId= / ?tecnicoId= / ?estado=; pagina por defecto 1, tamano 20.
    /// El kanban llama una vez por columna (?estado=) y usa totalRegistros como contador.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginadoDto<TicketResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginadoDto<TicketResponseDto>>> ObtenerTodos(
        [FromQuery] int? clienteId, [FromQuery] int? tecnicoId, [FromQuery] EstadoTicket? estado,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default)
        => Ok(await _service.ObtenerTodosAsync(clienteId, tecnicoId, estado, pagina, tamano, ct));

    [HttpGet("{id:int}", Name = "ObtenerTicketPorId")]
    [ProducesResponseType(typeof(TicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponseDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var ticket = await _service.ObtenerPorIdAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(typeof(TicketResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponseDto>> Crear([FromBody] CrearTicketDto dto, CancellationToken ct)
    {
        var creado = await _service.CrearAsync(dto, ct);
        return CreatedAtRoute("ObtenerTicketPorId", new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarTicketDto dto, CancellationToken ct)
    {
        await _service.ActualizarAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _service.EliminarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Cambia el estado de un ticket (máquina de estados en Domain/EstadoTicketTransiciones.cs).</summary>
    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(typeof(TicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponseDto>> CambiarEstado(int id, [FromBody] CambiarEstadoTicketDto dto, CancellationToken ct)
        => Ok(await _service.CambiarEstadoAsync(id, dto, ct));
}
