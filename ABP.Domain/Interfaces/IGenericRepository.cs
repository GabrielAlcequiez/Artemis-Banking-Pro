namespace ABP.Domain.Interfaces;

public interface IGenericRepository<TEntity, in TId>
    where TEntity : class
{
    IQueryable<TEntity> GetAllQueryable(bool trackChanges = false);

    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> UpdateAsync(
        TId id,
        TEntity value,
        CancellationToken cancellationToken = default);

    Task<TEntity?> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
