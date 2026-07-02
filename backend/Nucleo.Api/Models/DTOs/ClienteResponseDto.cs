namespace Nucleo.Api.Models.DTOs;

/// <summary>Forma en que el cliente se expone hacia afuera. Nunca devolvemos la entidad directamente.</summary>
public class ClienteResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
}
