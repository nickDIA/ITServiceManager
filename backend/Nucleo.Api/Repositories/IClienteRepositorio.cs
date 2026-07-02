using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Repositories;

/// <summary>
/// Repositorio específico de Cliente: hereda el CRUD genérico y agrega consultas
/// propias del dominio que el repo genérico no puede expresar.
/// </summary>
public interface IClienteRepositorio : IRepositorio<Cliente>
{
    /// <summary>¿Existe ya un cliente con este RFC? <paramref name="excluyendoId"/> permite ignorar el propio registro al actualizar.</summary>
    Task<bool> ExisteRfcAsync(string rfc, int? excluyendoId = null, CancellationToken ct = default);

    /// <summary>¿El cliente tiene activos asociados? (se usa para bloquear el borrado).</summary>
    Task<bool> TieneActivosAsync(int clienteId, CancellationToken ct = default);
}
