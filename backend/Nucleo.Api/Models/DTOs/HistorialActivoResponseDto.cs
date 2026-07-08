using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

public class HistorialActivoResponseDto
{
    public int Id { get; set; }
    public int ActivoId { get; set; }
    public EstadoActivo EstadoAnterior { get; set; }
    public EstadoActivo EstadoNuevo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int TecnicoId { get; set; }
    public string TecnicoNombre { get; set; } = string.Empty;
}
