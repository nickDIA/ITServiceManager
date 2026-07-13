using Nucleo.Api.Models.DTOs;

namespace Nucleo.Api.Services;

public interface IAuthService
{
    /// <summary>Verifica email + contraseña (BCrypt) y devuelve un JWT. Lanza CredencialesInvalidasException si no coinciden.</summary>
    Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
