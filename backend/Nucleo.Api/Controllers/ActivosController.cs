using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleo.Api.Common;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Services;

namespace Nucleo.Api.Controllers;

/// <summary>Requiere token (cualquier rol puede leer; escritura restringida a Admin/Tecnico).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivosController : ControllerBase
{
    private readonly IActivoService _service;

    public ActivosController(IActivoService service)
    {
        _service = service;
    }

    /// <summary>Lista activos. Filtra por cliente con ?clienteId=.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ActivoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivoResponseDto>>> ObtenerTodos([FromQuery] int? clienteId, CancellationToken ct)
        => Ok(await _service.ObtenerTodosAsync(clienteId, ct));

    [HttpGet("{id:int}", Name = "ObtenerActivoPorId")]
    [ProducesResponseType(typeof(ActivoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivoResponseDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var activo = await _service.ObtenerPorIdAsync(id, ct);
        return activo is null ? NotFound() : Ok(activo);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(typeof(ActivoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivoResponseDto>> Crear([FromBody] CrearActivoDto dto, CancellationToken ct)
    {
        var creado = await _service.CrearAsync(dto, ct);
        return CreatedAtRoute("ObtenerActivoPorId", new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarActivoDto dto, CancellationToken ct)
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

    /// <summary>
    /// Cambia el estado de un activo. Valida la transición (máquina de estados) y registra
    /// el cambio en HistorialActivo dentro de la misma transacción (ver ActivoService).
    /// El técnico que hace el cambio se toma del claim NameIdentifier del JWT, no del body.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(typeof(ActivoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivoResponseDto>> CambiarEstado(int id, [FromBody] CambiarEstadoActivoDto dto, CancellationToken ct)
    {
        var tecnicoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _service.CambiarEstadoAsync(id, dto, tecnicoId, ct));
    }

    /// <summary>Historial de cambios de estado de un activo, más reciente primero.</summary>
    [HttpGet("{id:int}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialActivoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HistorialActivoResponseDto>>> ObtenerHistorial(int id, CancellationToken ct)
        => Ok(await _service.ObtenerHistorialAsync(id, ct));
}
