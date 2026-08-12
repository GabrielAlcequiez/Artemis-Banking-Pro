using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Commerce;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.Application.UnitTests.Features.Commerce;

internal sealed class CommerceRepositoryStub : ICommerceRepository
{
    public PagedResult<CommerceSummaryReadModel> SearchResult { get; set; } =
        new(Array.Empty<CommerceSummaryReadModel>(), 1, 20, 0);

    public CommerceDetailReadModel? DetailResult { get; set; }
    public bool EmailExistsResult { get; set; }
    public bool RncExistsResult { get; set; }
    public CommerceEntity? CommerceForUpdate { get; set; }
    public CommerceEntity? AddedCommerce { get; private set; }
    public int? ReceivedPage { get; private set; }
    public int? ReceivedPageSize { get; private set; }
    public CommerceStatusFilter? ReceivedStatus { get; private set; }
    public Guid? ReceivedCommerceId { get; private set; }
    public string? ReceivedEmail { get; private set; }
    public string? ReceivedRnc { get; private set; }
    public Guid? ReceivedEmailExclusion { get; private set; }
    public Guid? ReceivedRncExclusion { get; private set; }
    public int EmailExistsCalls { get; private set; }
    public int RncExistsCalls { get; private set; }
    public int AddCalls { get; private set; }

    public Task<PagedResult<CommerceSummaryReadModel>> SearchAsync(
        int page,
        int pageSize,
        CommerceStatusFilter? status = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedPage = page;
        ReceivedPageSize = pageSize;
        ReceivedStatus = status;
        return Task.FromResult(SearchResult);
    }

    public Task<CommerceDetailReadModel?> GetDetailsAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default)
    {
        ReceivedCommerceId = commerceId;
        return Task.FromResult(DetailResult);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        Guid? excludingCommerceId = null,
        CancellationToken cancellationToken = default)
    {
        EmailExistsCalls++;
        ReceivedEmail = email;
        ReceivedEmailExclusion = excludingCommerceId;
        return Task.FromResult(EmailExistsResult);
    }

    public Task<bool> RncExistsAsync(
        string rnc,
        Guid? excludingCommerceId = null,
        CancellationToken cancellationToken = default)
    {
        RncExistsCalls++;
        ReceivedRnc = rnc;
        ReceivedRncExclusion = excludingCommerceId;
        return Task.FromResult(RncExistsResult);
    }

    public Task<CommerceEntity?> GetForUpdateAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default)
    {
        ReceivedCommerceId = commerceId;
        return Task.FromResult(CommerceForUpdate);
    }

    public IQueryable<CommerceEntity> GetAllQueryable(bool trackChanges = false) =>
        throw new NotImplementedException();

    public Task<CommerceEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<CommerceEntity>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<CommerceEntity> AddAsync(
        CommerceEntity entity,
        CancellationToken cancellationToken = default)
    {
        AddCalls++;
        AddedCommerce = entity;
        return Task.FromResult(entity);
    }

    public Task<CommerceEntity?> UpdateAsync(Guid id, CommerceEntity value, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<CommerceEntity?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
