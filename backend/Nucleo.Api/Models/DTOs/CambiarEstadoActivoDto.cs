using System.ComponentModel.DataAnnotations;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// Datos de entrada para cambiar el estado de un activo. TecnicoId viaja explícito en el
/// body porque todavía no existe autenticación (Fase 3): cuando se agregue JWT, este campo
/// se eliminará del DTO y el técnico se tomará del claim del token.
/// </summary>
public class CambiarEstadoActivoDto
{
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    public EstadoActivo NuevoEstado { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El técnico es obligatorio.")]
    public int TecnicoId { get; set; }
}
