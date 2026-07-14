using System.ComponentModel.DataAnnotations;
using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Models.DTOs;

public class CambiarEstadoTicketDto
{
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    public EstadoTicket NuevoEstado { get; set; }
}
