using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Data;

namespace Nucleo.Api.Repositories;

/// <summary>
/// Implementación genérica de <see cref="IRepositorio{T}"/> sobre EF Core.
/// Es la ÚNICA capa que conoce el <see cref="AppDbContext"/> / EF. Sin lógica de negocio.
/// </summary>
public class Repositorio<T> : IRepositorio<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repositorio(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // Lectura de solo lectura: AsNoTracking evita el costo de rastrear entidades que no se van a modificar.
    public async Task<IReadOnlyList<T>> ObtenerTodosAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().ToListAsync(ct);

    // FindAsync rastrea la entidad (la necesitamos rastreada para actualizar/eliminar).
    public async Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public async Task AgregarAsync(T entidad, CancellationToken ct = default)
        => await _dbSet.AddAsync(entidad, ct);

    public void Actualizar(T entidad) => _dbSet.Update(entidad);

    public void Eliminar(T entidad) => _dbSet.Remove(entidad);

    public async Task<bool> ExisteAsync(int id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct) is not null;

    public async Task<int> GuardarCambiosAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
