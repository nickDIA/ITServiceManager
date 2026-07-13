using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleo.Api.Common;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Services;

namespace Nucleo.Api.Controllers;

/// <summary>
/// Frontera HTTP de Cliente. Solo traduce HTTP &lt;-&gt; servicio: no contiene lógica de negocio
/// ni toca EF. Las reglas de negocio viven en <see cref="IClienteService"/>.
/// Requiere token (cualquier rol puede leer; escritura restringida a Admin/Tecnico).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;

    public ClientesController(IClienteService service)
    {
        _service = service;
    }

    /// <summary>Lista todos los clientes.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClienteResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteResponseDto>>> ObtenerTodos(CancellationToken ct)
        => Ok(await _service.ObtenerTodosAsync(ct));

    /// <summary>Obtiene un cliente por id.</summary>
    [HttpGet("{id:int}", Name = "ObtenerClientePorId")]
    [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponseDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var cliente = await _service.ObtenerPorIdAsync(id, ct);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    /// <summary>Crea un cliente.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteResponseDto>> Crear([FromBody] CrearClienteDto dto, CancellationToken ct)
    {
        var creado = await _service.CrearAsync(dto, ct);
        return CreatedAtRoute("ObtenerClientePorId", new { id = creado.Id }, creado);
    }

    /// <summary>Actualiza un cliente existente.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarClienteDto dto, CancellationToken ct)
    {
        await _service.ActualizarAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>Elimina un cliente.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Escritura)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _service.EliminarAsync(id, ct);
        return NoContent();
    }
}
