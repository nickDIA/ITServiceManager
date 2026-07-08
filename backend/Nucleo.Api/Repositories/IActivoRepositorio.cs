using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public interface IActivoRepositorio : IRepositorio<Activo>
{
    /// <summary>Lista de activos con su Cliente cargado (join), opcionalmente filtrada por cliente.</summary>
    Task<IReadOnlyList<Activo>> ObtenerTodosConClienteAsync(int? clienteId, CancellationToken ct = default);

    /// <summary>Un activo con su Cliente cargado (join). Solo lectura: usar ObtenerPorIdAsync si se va a modificar.</summary>
    Task<Activo?> ObtenerPorIdConClienteAsync(int id, CancellationToken ct = default);

    Task<bool> ExisteNumeroSerieAsync(string numeroSerie, int? excluyendoId = null, CancellationToken ct = default);
}
