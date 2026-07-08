using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

public class ActivoResponseDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public TipoActivo Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NumeroSerie { get; set; } = string.Empty;
    public EstadoActivo Estado { get; set; }
    public DateTime FechaAdquisicion { get; set; }
}
