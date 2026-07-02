namespace Nucleo.Api.Common.Exceptions;

/// <summary>
/// Se lanza ante una violación de regla de negocio que choca con el estado actual
/// (RFC duplicado, intentar borrar un cliente con activos, etc.). El handler global la mapea a 409.
/// </summary>
public class ConflictoException : Exception
{
    public ConflictoException(string mensaje) : base(mensaje)
    {
    }
}
