namespace Nucleo.Api.Models.Entities;

/// <summary>Contrato de retainer mensual entre el proveedor y un cliente.</summary>
public class Contrato
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public decimal TarifaMensual { get; set; }

    public int HorasIncluidas { get; set; }

    /// <summary>SLA de respuesta en horas.</summary>
    public int SlaHoras { get; set; }

    public DateTime FechaInicio { get; set; }

    public bool Activo { get; set; } = true;

    // --- Navegación ---
    public Cliente Cliente { get; set; } = null!;
}
