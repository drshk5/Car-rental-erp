using System.Collections.Concurrent;
using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Common;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Infrastructure.Persistence;

public sealed class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly ConcurrentDictionary<Guid, TEntity> _store;

    public InMemoryRepository(ConcurrentDictionary<Guid, TEntity> store)
    {
        _store = store;
    }

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyCollection<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TEntity> items = _store.Values.OrderByDescending(x => x.CreatedAtUtc).ToArray();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryShaper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryShaper);

        IReadOnlyCollection<TResult> items = queryShaper(_store.Values.AsQueryable()).ToArray();
        return Task.FromResult(items);
    }

    public Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _store.Values
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return Task.FromResult(new PagedResult<TEntity>(items, _store.Count, page, pageSize));
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }
}
