using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Services;

namespace Nucleo.Api.Controllers;

/// <summary>Solo lectura: existe para poblar los selectores de asignación del frontend.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TecnicosController : ControllerBase
{
    private readonly ITecnicoService _service;

    public TecnicosController(ITecnicoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TecnicoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TecnicoResponseDto>>> ObtenerTodos(CancellationToken ct)
        => Ok(await _service.ObtenerTodosAsync(ct));
}
