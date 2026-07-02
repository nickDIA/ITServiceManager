namespace Nucleo.Api.Models.Entities;

/// <summary>PYME atendida por el proveedor de servicios IT.</summary>
public class Cliente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>RFC de la empresa. Único en el sistema.</summary>
    public string Rfc { get; set; } = string.Empty;

    public string? Contacto { get; set; }

    public string? Telefono { get; set; }

    /// <summary>Bandera de habilitado/deshabilitado (baja lógica del cliente).</summary>
    public bool Activo { get; set; } = true;

    // --- Navegación ---
    public ICollection<Activo> Activos { get; set; } = new List<Activo>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}
