using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Mapping;
using ABP.Application.Features.Commerce.Queries.GetCommerces;
using ABP.Application.Features.Commerce.Validation;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.Commerce;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Commerce.Queries;

public sealed class GetCommercesQueryTests
{
    [Fact]
    public async Task Validator_reuses_shared_list_rules()
    {
        var validator = new GetCommercesQueryValidator(new CommerceListRequestValidator());
        var query = new GetCommercesQuery(new CommerceListRequest(PageSize: 21));

        var result = await validator.ValidateAsync(query);

        Assert.Contains(result.Errors, error => error.PropertyName == "Request.PageSize");
    }

    [Fact]
    public async Task Handler_passes_filters_and_maps_the_page()
    {
        var summary = new CommerceSummaryReadModel(
            Guid.NewGuid(), "Tienda Demo", null, "contacto@tiendademo.com",
            "8095551234", "101999999", CommerceStatus.Inactive, true, DateTimeOffset.UtcNow);
        var repository = new CommerceRepositoryStub
        {
            SearchResult = new PagedResult<CommerceSummaryReadModel>([summary], 2, 10, 15)
        };
        var handler = new GetCommercesQueryHandler(repository, CreateMapper());

        var result = await handler.Handle(
            new GetCommercesQuery(new CommerceListRequest(2, 10, CommerceStatusFilter.All)),
            CancellationToken.None);

        Assert.Equal(2, repository.ReceivedPage);
        Assert.Equal(10, repository.ReceivedPageSize);
        Assert.Equal(CommerceStatusFilter.All, repository.ReceivedStatus);
        Assert.Equal(15, result.TotalRecords);
        Assert.Equal(2, result.TotalPages);
        var item = Assert.Single(result.Data);
        Assert.Equal(summary.Id, item.Id);
        Assert.False(item.IsActive);
        Assert.True(item.HasAssociatedUser);
    }

    private static IMapper CreateMapper() => new MapperConfiguration(
        configuration => configuration.AddProfile<CommerceProfile>(),
        NullLoggerFactory.Instance).CreateMapper();
}
