using System.ComponentModel.DataAnnotations;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// Datos de entrada para registrar un activo nuevo. No incluye Estado: todo activo nace
/// en <see cref="EstadoActivo.Operativo"/>; los cambios de estado posteriores pasan
/// siempre por el endpoint dedicado de cambio de estado (auditado).
/// </summary>
public class CrearActivoDto
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    public int ClienteId { get; set; }

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
