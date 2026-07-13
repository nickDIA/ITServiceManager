using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Services;

/// <summary>
/// Encapsula la generación de JWT. Aislado del resto del Service para que AuthService
/// no mezcle "verificar credenciales" con "cómo se firma un token".
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraEn) GenerarToken(Tecnico tecnico)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Falta configurar Jwt:Key. En desarrollo: dotnet user-secrets set \"Jwt:Key\" \"<valor>\".");

        var minutos = jwtSection.GetValue("ExpiresInMinutes", 120);
        var expiraEn = DateTime.UtcNow.AddMinutes(minutos);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tecnico.Id.ToString()),
            new(ClaimTypes.Email, tecnico.Email),
            new(ClaimTypes.Name, tecnico.Nombre),
            new(ClaimTypes.Role, tecnico.Rol.ToString())
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
