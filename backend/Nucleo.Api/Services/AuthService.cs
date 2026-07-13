using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Repositories;

namespace Nucleo.Api.Services;

public class AuthService : IAuthService
{
    private readonly ITecnicoRepositorio _tecnicoRepositorio;
    private readonly ITokenService _tokenService;

    public AuthService(ITecnicoRepositorio tecnicoRepositorio, ITokenService tokenService)
    {
        _tecnicoRepositorio = tecnicoRepositorio;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var tecnico = await _tecnicoRepositorio.ObtenerPorEmailAsync(dto.Email.Trim(), ct);

        // Mismo mensaje de error tanto si el email no existe como si la contraseña es incorrecta:
        // no revelar cuál de las dos cosas falló es una práctica estándar de seguridad.
        if (tecnico is null || !BCrypt.Net.BCrypt.Verify(dto.Password, tecnico.PasswordHash))
            throw new CredencialesInvalidasException();

        var (token, expiraEn) = _tokenService.GenerarToken(tecnico);

        return new LoginResponseDto
        {
            Token = token,
            ExpiraEn = expiraEn,
            TecnicoId = tecnico.Id,
            Nombre = tecnico.Nombre,
            Email = tecnico.Email,
            Rol = tecnico.Rol
        };
    }
}
