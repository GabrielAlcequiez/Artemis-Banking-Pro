using ABP.Domain.Common;

namespace ABP.Application.Interfaces.Services;

/// <summary>
/// Provides reusable CRUD operations for simple maintenance use cases.
/// </summary>
/// <remarks>
/// Financial operations must use dedicated commands, handlers, and transactional services.
/// </remarks>
public interface IGenericService<TDto, TEntity, TKey>
    where TDto : class
    where TEntity : BaseEntity<TKey>
{
    Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
