using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>Técnico expuesto hacia afuera. Nunca incluye PasswordHash.</summary>
public class TecnicoResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolTecnico Rol { get; set; }
}
