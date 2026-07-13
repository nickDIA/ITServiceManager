using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Services;

public interface ITokenService
{
    /// <summary>Genera un JWT firmado con los claims del técnico (id, email, nombre, rol).</summary>
    (string Token, DateTime ExpiraEn) GenerarToken(Tecnico tecnico);
}
