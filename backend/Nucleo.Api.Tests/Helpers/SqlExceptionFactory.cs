using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Nucleo.Api.Tests.Helpers;

/// <summary>
/// SqlException no tiene constructor público (solo lo crea el driver al hablar con SQL Server
/// real), pero ActivoService distingue el error 547 (violación de FK) para traducirlo a 404.
/// Este helper lo fabrica vía reflexión para poder probar esa rama sin base de datos.
/// Si una actualización de Microsoft.Data.SqlClient cambia las firmas internas, fallará
/// aquí con un mensaje claro (y solo se rompen los tests que lo usan).
/// </summary>
public static class SqlExceptionFactory
{
    public static SqlException Crear(int numeroDeError)
    {
        // SqlError: tomar el constructor interno de más parámetros y rellenar por tipo;
        // el primer int es siempre infoNumber (el número de error que nos interesa).
        var errorCtor = typeof(SqlError)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var yaAsignamosNumero = false;
        var args = errorCtor.GetParameters().Select(p =>
        {
            if (p.ParameterType == typeof(int) && !yaAsignamosNumero)
            {
                yaAsignamosNumero = true;
                return (object?)numeroDeError;
            }
            if (p.ParameterType == typeof(string)) return "";
            if (p.ParameterType.IsValueType) return Activator.CreateInstance(p.ParameterType);
            return null;
        }).ToArray();

        var error = errorCtor.Invoke(args);

        var coleccion = Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)
            ?? throw new InvalidOperationException("No se pudo crear SqlErrorCollection por reflexión.");

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(coleccion, [error]);

        var crearExcepcion = typeof(SqlException)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(m => m.Name == "CreateException" && m.GetParameters().Length == 2
                        && m.GetParameters()[0].ParameterType == typeof(SqlErrorCollection)
                        && m.GetParameters()[1].ParameterType == typeof(string));

        return (SqlException)crearExcepcion.Invoke(null, [coleccion, "test"])!;
    }
}
