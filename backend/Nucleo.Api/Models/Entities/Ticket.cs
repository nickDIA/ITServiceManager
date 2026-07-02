namespace Nucleo.Api.Models.Entities;

/// <summary>Ticket de servicio levantado para un cliente, opcionalmente ligado a un activo.</summary>
public class Ticket
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    /// <summary>Activo afectado (opcional: un ticket puede ser general del cliente).</summary>
    public int? ActivoId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public Prioridad Prioridad { get; set; } = Prioridad.Media;

    public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;

    /// <summary>Técnico asignado.</summary>
    public int TecnicoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    // --- Navegación ---
    public Cliente Cliente { get; set; } = null!;
    public Activo? Activo { get; set; }
    public Tecnico Tecnico { get; set; } = null!;
}
