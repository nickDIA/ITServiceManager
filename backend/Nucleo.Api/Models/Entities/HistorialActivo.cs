namespace Nucleo.Api.Models.Entities;

/// <summary>
/// Registro de auditoría de un cambio de estado de un activo.
/// Se crea SIEMPRE dentro de la misma transacción que el cambio de estado (requisito 6).
/// </summary>
public class HistorialActivo
{
    public int Id { get; set; }

    public int ActivoId { get; set; }

    public EstadoActivo EstadoAnterior { get; set; }

    public EstadoActivo EstadoNuevo { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    /// <summary>Técnico que realizó el cambio.</summary>
    public int TecnicoId { get; set; }

    // --- Navegación ---
    public Activo Activo { get; set; } = null!;
    public Tecnico Tecnico { get; set; } = null!;
}
