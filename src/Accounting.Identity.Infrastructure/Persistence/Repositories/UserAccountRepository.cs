using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Interfaces;
using Accounting.Identity.Infrastructure.Persistence.Entities;
using Accounting.Identity.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UserAccount aggregate
/// </summary>
public class UserAccountRepository : IUserAccountRepository
{
    private readonly IdentityDbContext _context;

    public UserAccountRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccount?> GetByIdAsync(UserAccountId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<UserAccountEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken);

        return entity != null ? UserAccountMapper.ToDomain(entity) : null;
    }

    public async Task<UserAccount?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<UserAccountEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email.Value, cancellationToken);

        return entity != null ? UserAccountMapper.ToDomain(entity) : null;
    }

    public async Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserAccountEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Email == email.Value, cancellationToken);
    }

    public async Task AddAsync(UserAccount aggregate, CancellationToken cancellationToken = default)
    {
        var entity = UserAccountMapper.ToEntity(aggregate);
        await _context.Set<UserAccountEntity>().AddAsync(entity, cancellationToken);
    }

    public void Update(UserAccount aggregate)
    {
        var entity = UserAccountMapper.ToEntity(aggregate);
        _context.Set<UserAccountEntity>().Update(entity);
    }

    public void Remove(UserAccount aggregate)
    {
        var entity = new UserAccountEntity { Id = aggregate.Id.Value };
        _context.Set<UserAccountEntity>().Remove(entity);
    }
}
