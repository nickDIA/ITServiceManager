using System.ComponentModel.DataAnnotations;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// Datos de entrada para actualizar los datos descriptivos de un activo.
/// Deliberadamente NO incluye Estado ni ClienteId: el estado solo cambia vía el endpoint
/// transaccional dedicado, y un activo no se reasigna de cliente por este medio.
/// </summary>
public class ActualizarActivoDto
{
    [Required(ErrorMessage = "El tipo de activo es obligatorio.")]
    public TipoActivo Tipo { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de serie es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El número de serie debe tener entre 3 y 100 caracteres.")]
    public string NumeroSerie { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de adquisición es obligatoria.")]
    public DateTime FechaAdquisicion { get; set; }
}
