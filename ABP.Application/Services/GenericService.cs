using ABP.Application.Interfaces.Services;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using AutoMapper;

namespace ABP.Application.Services
{
    public class GenericService<TDto, TEntity, TKey> : IGenericService<TDto, TEntity, TKey>
        where TDto : class
        where TEntity : BaseEntity<TKey>
    {

        protected readonly IGenericRepository<TEntity, TKey> _repository;
        protected readonly IMapper _mapper;

        public GenericService(IGenericRepository<TEntity, TKey> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
         public async Task<IReadOnlyCollection<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetAllAsync(false, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<TDto>>(entities);
        }

        public async Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return _mapper.Map<TDto>(entity);
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAsync(id);
        }

    

    }
}