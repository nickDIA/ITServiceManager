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

        // Claims cortos (sub/email/name/role) en vez de los URIs largos de ClaimTypes.*:
        // el frontend Angular decodifica el JWT para leer estos valores, y las claves
        // estándar de JWT son mucho más limpias de consumir que los identificadores SOAP
        // heredados (http://schemas.xmlsoap.org/...). RoleClaimType/NameClaimType se
        // reconfiguran a juego en Program.cs para que [Authorize(Roles=...)] los reconozca.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, tecnico.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, tecnico.Email),
            new("name", tecnico.Nombre),
            new("role", tecnico.Rol.ToString())
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
