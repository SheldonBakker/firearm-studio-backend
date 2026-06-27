using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Persistence;

public sealed class Repository<TEntity>(IApplicationDbContext db) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default) =>
        await db.Set<TEntity>().ToListAsync(ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default) =>
        await db.Set<TEntity>().AddAsync(entity, ct);

    public void Update(TEntity entity) => db.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => db.Set<TEntity>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
