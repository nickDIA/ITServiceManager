using System.ComponentModel.DataAnnotations;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// Datos de entrada para actualizar un ticket: descripción, prioridad, activo relacionado
/// y reasignación de técnico. NO incluye Estado ni ClienteId (el cliente dueño no cambia).
/// </summary>
public class ActualizarTicketDto
{
    public int? ActivoId { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "La descripción debe tener entre 5 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La prioridad es obligatoria.")]
    public Prioridad Prioridad { get; set; }

    [Required(ErrorMessage = "El técnico asignado es obligatorio.")]
    public int TecnicoId { get; set; }
}
