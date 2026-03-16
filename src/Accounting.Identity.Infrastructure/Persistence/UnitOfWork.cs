using Accounting.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Accounting.Identity.Infrastructure.Persistence;

/// <summary>
/// Implementation of Unit of Work pattern using Entity Framework Core.
/// Manages database transactions and ensures atomicity of operations.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync()
    {
        try
        {
            await (_currentTransaction?.RollbackAsync() ?? Task.CompletedTask);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
