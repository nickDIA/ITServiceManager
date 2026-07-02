namespace Nucleo.Api.Models.Entities;

/// <summary>Usuario del sistema (técnico) que atiende tickets y cambia el estado de los activos.</summary>
public class Tecnico
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo, usado como login. Único en el sistema.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash BCrypt de la contraseña (se poblará en la fase de autenticación).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public RolTecnico Rol { get; set; } = RolTecnico.Tecnico;

    // --- Navegación ---
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<HistorialActivo> CambiosRegistrados { get; set; } = new List<HistorialActivo>();
}
