using System.ComponentModel.DataAnnotations;

namespace Nucleo.Api.Models.DTOs;

/// <summary>Datos de entrada para actualizar un cliente (reemplazo completo vía PUT).</summary>
public class ActualizarClienteDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RFC es obligatorio.")]
    [RegularExpression(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$",
        ErrorMessage = "El RFC no tiene un formato válido (ej. DNO950101AB1).")]
    public string Rfc { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Contacto { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    /// <summary>Permite activar/desactivar el cliente (baja lógica).</summary>
    public bool Activo { get; set; }
}
