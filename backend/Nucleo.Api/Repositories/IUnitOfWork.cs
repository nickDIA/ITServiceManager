namespace Nucleo.Api.Repositories;

/// <summary>
/// Da al Service control explícito sobre la transacción de la unidad de trabajo actual,
/// sin exponerle tipos de EF Core (IDbContextTransaction, DbContext). Existe porque el
/// patrón de repositorio (un GuardarCambiosAsync por repo) hace que una operación que
/// toca dos repos (p. ej. Activo + HistorialActivo) dispare dos SaveChanges separados,
/// que NO son atómicos entre sí a menos que se envuelvan en una transacción explícita.
/// </summary>
public interface IUnitOfWork
{
    Task IniciarTransaccionAsync(CancellationToken ct = default);

    Task ConfirmarTransaccionAsync(CancellationToken ct = default);

    Task RevertirTransaccionAsync(CancellationToken ct = default);
}
