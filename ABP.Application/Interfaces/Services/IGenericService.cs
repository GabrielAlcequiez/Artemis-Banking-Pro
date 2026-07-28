namespace ABP.Application.Interfaces.Services;

/// <summary>
/// Provides reusable CRUD operations for simple maintenance use cases.
/// </summary>
/// <remarks>
/// Financial operations must use dedicated commands, handlers, and transactional services.
/// </remarks>
public interface IGenericService<TEntity, in TId>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
