using Microsoft.EntityFrameworkCore.Storage;
using Nucleo.Api.Data;

namespace Nucleo.Api.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaccionActual;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task IniciarTransaccionAsync(CancellationToken ct = default)
        => _transaccionActual = await _context.Database.BeginTransactionAsync(ct);

    public async Task ConfirmarTransaccionAsync(CancellationToken ct = default)
    {
        if (_transaccionActual is null)
            return;

        await _transaccionActual.CommitAsync(ct);
        await _transaccionActual.DisposeAsync();
        _transaccionActual = null;
    }

    public async Task RevertirTransaccionAsync(CancellationToken ct = default)
    {
        if (_transaccionActual is null)
            return;

        await _transaccionActual.RollbackAsync(ct);
        await _transaccionActual.DisposeAsync();
        _transaccionActual = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaccionActual is not null)
            await _transaccionActual.DisposeAsync();
    }
}
