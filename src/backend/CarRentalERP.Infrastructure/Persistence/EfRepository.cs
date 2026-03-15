using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Common;
using CarRentalERP.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarRentalERP.Infrastructure.Persistence;

public sealed class EfRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<TEntity> _set;

    public EfRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _set = dbContext.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _set
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryShaper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryShaper);

        return await queryShaper(_set.AsQueryable()).ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : pageSize;
        var total = await _set.CountAsync(cancellationToken);
        var items = await _set
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TEntity>(items, total, safePage, safePageSize);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _set.Update(entity);
        return Task.CompletedTask;
    }
}
