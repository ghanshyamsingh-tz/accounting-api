namespace Accounting.Identity.Domain.Interfaces;

/// <summary>
/// Generic repository interface for aggregate roots.
/// Repositories are responsible for aggregate lifecycle management (add, get, delete).
/// They work with aggregate roots only - never with entities inside aggregates.
/// </summary>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public interface IRepository<TAggregate, TId> 
    where TAggregate : class
    where TId : notnull
{
    /// <summary>
    /// Gets an aggregate by its identifier.
    /// </summary>
    /// <param name="id">The aggregate's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate if found; otherwise null.</returns>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate in the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to update.</param>
    void Update(TAggregate aggregate);

    /// <summary>
    /// Removes an aggregate from the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to remove.</param>
    void Remove(TAggregate aggregate);
}
