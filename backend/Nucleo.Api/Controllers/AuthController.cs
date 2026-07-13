using Microsoft.AspNetCore.Mvc;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Services;

namespace Nucleo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    /// <summary>Login. Sin [Authorize]: este endpoint es público por definición.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
        => Ok(await _service.LoginAsync(dto, ct));
}
