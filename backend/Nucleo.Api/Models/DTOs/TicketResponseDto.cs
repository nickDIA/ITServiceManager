using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// El "reporte con JOINs" de la spec: trae nombre de cliente + activo + técnico ya resueltos
/// (3 Include en TicketRepositorio), no solo los IDs crudos.
/// </summary>
public class TicketResponseDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? ActivoId { get; set; }
    public string? ActivoNombre { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Prioridad Prioridad { get; set; }
    public EstadoTicket Estado { get; set; }
    public int TecnicoId { get; set; }
    public string TecnicoNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCierre { get; set; }
}
