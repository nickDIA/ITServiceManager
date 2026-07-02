namespace Nucleo.Api.Models.Entities;

/// <summary>Activo IT (hardware, software o equipo de red) perteneciente a un cliente.</summary>
public class Activo
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public TipoActivo Tipo { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Número de serie. Único en el sistema.</summary>
    public string NumeroSerie { get; set; } = string.Empty;

    public EstadoActivo Estado { get; set; } = EstadoActivo.Operativo;

    public DateTime FechaAdquisicion { get; set; }

    // --- Navegación ---
    public Cliente Cliente { get; set; } = null!;
    public ICollection<HistorialActivo> Historial { get; set; } = new List<HistorialActivo>();
}
