using ABP.Domain.Common;

namespace ABP.Domain.Interfaces;

public interface IGenericRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
{
    IQueryable<TEntity> GetAllQueryable(bool trackChanges = false);

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> UpdateAsync(
        TKey id,
        TEntity value,
        CancellationToken cancellationToken = default);

    Task<TEntity?> DeleteAsync(
        TKey id,
        CancellationToken cancellationToken = default);
}