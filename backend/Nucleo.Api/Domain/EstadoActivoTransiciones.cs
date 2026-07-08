using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Domain;

/// <summary>
/// Máquina de estados de un Activo: única fuente de verdad sobre qué transiciones de
/// <see cref="EstadoActivo"/> son válidas. El servicio la consulta ANTES de abrir
/// cualquier transacción; si la transición no es válida, no llega a tocar la base de datos.
/// </summary>
public static class EstadoActivoTransiciones
{
    private static readonly Dictionary<EstadoActivo, EstadoActivo[]> Permitidas = new()
    {
        [EstadoActivo.Operativo] = [EstadoActivo.EnReparacion, EstadoActivo.EnAlmacen, EstadoActivo.Retirado],
        [EstadoActivo.EnReparacion] = [EstadoActivo.Operativo, EstadoActivo.EnAlmacen, EstadoActivo.Retirado],
        [EstadoActivo.EnAlmacen] = [EstadoActivo.Operativo, EstadoActivo.EnReparacion, EstadoActivo.Retirado],
        // Retirado es un estado terminal: un activo dado de baja no vuelve a cambiar.
        [EstadoActivo.Retirado] = []
    };

    public static bool EsValida(EstadoActivo actual, EstadoActivo nuevo)
        => actual != nuevo && Permitidas[actual].Contains(nuevo);

    public static IReadOnlyList<EstadoActivo> TransicionesDesde(EstadoActivo actual)
        => Permitidas[actual];
}
