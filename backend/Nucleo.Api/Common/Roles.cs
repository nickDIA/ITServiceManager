namespace Nucleo.Api.Common;

/// <summary>
/// Constantes de rol para usar en [Authorize(Roles = ...)], evitando strings mágicos
/// repetidos en los controllers. Deben coincidir exactamente con los nombres de
/// <see cref="Models.Entities.RolTecnico"/> (el claim de rol en el JWT usa ToString() del enum).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Tecnico = "Tecnico";
    public const string Lector = "Lector";

    /// <summary>Roles con permiso de escritura (crear/actualizar/eliminar/cambiar estado). Lector queda fuera.</summary>
    public const string Escritura = Admin + "," + Tecnico;
}
