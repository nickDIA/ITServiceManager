namespace Nucleo.Api.Common.Exceptions;

/// <summary>
/// Se lanza cuando el login falla (email no existe o contraseña incorrecta). El mensaje es
/// deliberadamente genérico en ambos casos: no revelar si el email existe es una práctica
/// estándar de seguridad. El handler global la mapea a 401.
/// </summary>
public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() : base("Email o contraseña incorrectos.")
    {
    }
}
