using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly AppDbContext _context;
        protected DbSet<TEntity> Entities => _context.Set<TEntity>();

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<TEntity> GetAllQueryable(bool trackChanges = false)
        {
            return trackChanges
                ? Entities
                : Entities.AsNoTracking();
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var result = await Entities.AddAsync(entity, cancellationToken);
            return result.Entity;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default)
        {
            return await GetAllQueryable(trackChanges)
                .ToListAsync(cancellationToken);
        }

        public async Task<TEntity?> GetByIdAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            return await Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    entity => EF.Property<TKey>(entity, "Id")!.Equals(id),
                    cancellationToken);
        }

        public async Task<TEntity?> UpdateAsync(
            TKey id,
            TEntity value,
            CancellationToken cancellationToken = default)
        {
            var existing = await Entities.FirstOrDefaultAsync(
                entity => EF.Property<TKey>(entity, "Id")!.Equals(id),
                cancellationToken);

            if (existing is null)
            {
                return null;
            }

            var existingEntry = _context.Entry(existing);
            var incomingValues = _context.Entry(value).CurrentValues;

            foreach (var property in existingEntry.Properties)
            {
                if (property.Metadata.IsPrimaryKey() ||
                    property.Metadata.PropertyInfo is null ||
                    property.Metadata.Name is "CreatedAtUtc" or "CreatedByUserId")
                {
                    continue;
                }

                property.CurrentValue = incomingValues[property.Metadata.Name];
            }

            return existing;
        }

        // PENDIENTE
        public Task<TEntity?> DeleteAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
