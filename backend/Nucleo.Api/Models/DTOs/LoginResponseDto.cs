using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public int TecnicoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolTecnico Rol { get; set; }
}
