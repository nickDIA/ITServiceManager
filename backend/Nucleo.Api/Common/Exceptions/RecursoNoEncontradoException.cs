namespace Nucleo.Api.Common.Exceptions;

/// <summary>Se lanza cuando una operación referencia un recurso que no existe. El handler global la mapea a 404.</summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje)
    {
    }

    public RecursoNoEncontradoException(string recurso, object id)
        : base($"No se encontró {recurso} con id '{id}'.")
    {
    }
}
