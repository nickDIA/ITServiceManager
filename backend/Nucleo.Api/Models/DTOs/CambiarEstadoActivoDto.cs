using System.ComponentModel.DataAnnotations;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// Datos de entrada para cambiar el estado de un activo. El técnico que realiza el cambio
/// ya NO viaja en el body: sale del claim NameIdentifier del JWT (ver ActivosController).
/// </summary>
public class CambiarEstadoActivoDto
{
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    public EstadoActivo NuevoEstado { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
