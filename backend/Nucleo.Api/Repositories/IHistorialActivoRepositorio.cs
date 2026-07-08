using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

public interface IHistorialActivoRepositorio : IRepositorio<HistorialActivo>
{
    /// <summary>Historial de un activo, más reciente primero, con el Técnico cargado (join).</summary>
    Task<IReadOnlyList<HistorialActivo>> ObtenerPorActivoIdAsync(int activoId, CancellationToken ct = default);
}
