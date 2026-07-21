using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public interface IActivoRepositorio : IRepositorio<Activo>
{
    /// <summary>
    /// Página de activos con su Cliente cargado (join), opcionalmente filtrada por cliente,
    /// más el total de registros que cumplen el filtro.
    /// </summary>
    Task<(IReadOnlyList<Activo> Items, int Total)> ObtenerPaginadoConClienteAsync(
        int pagina, int tamano, int? clienteId, CancellationToken ct = default);

    /// <summary>Un activo con su Cliente cargado (join). Solo lectura: usar ObtenerPorIdAsync si se va a modificar.</summary>
    Task<Activo?> ObtenerPorIdConClienteAsync(int id, CancellationToken ct = default);

    Task<bool> ExisteNumeroSerieAsync(string numeroSerie, int? excluyendoId = null, CancellationToken ct = default);
}
