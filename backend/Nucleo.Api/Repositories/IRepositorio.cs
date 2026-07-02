namespace Nucleo.Api.Repositories;

/// <summary>
/// Contrato genérico de acceso a datos para cualquier entidad con clave entera.
/// Nota: las operaciones de escritura NO persisten por sí solas; hay que llamar a
/// <see cref="GuardarCambiosAsync"/>. Esto permite que el servicio agrupe varias
/// operaciones en una sola unidad de trabajo / transacción (clave para el requisito 6).
/// </summary>
public interface IRepositorio<T> where T : class
{
    Task<IReadOnlyList<T>> ObtenerTodosAsync(CancellationToken ct = default);

    Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task AgregarAsync(T entidad, CancellationToken ct = default);

    void Actualizar(T entidad);

    void Eliminar(T entidad);

    Task<bool> ExisteAsync(int id, CancellationToken ct = default);

    Task<int> GuardarCambiosAsync(CancellationToken ct = default);
}
