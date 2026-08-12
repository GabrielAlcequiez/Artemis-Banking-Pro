using ABP.Application.Features.Commerce.Mapping;
using ABP.Application.Features.Commerce.Queries.GetCommerceDetail;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.Commerce;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Commerce.Queries;

public sealed class GetCommerceDetailQueryTests
{
    [Fact]
    public async Task Existing_commerce_is_mapped_with_its_associated_user()
    {
        var commerceId = Guid.NewGuid();
        var repository = new CommerceRepositoryStub { DetailResult = CreateDetail(commerceId) };
        var handler = new GetCommerceDetailQueryHandler(repository, CreateMapper());

        var result = await handler.Handle(new GetCommerceDetailQuery(commerceId), CancellationToken.None);

        Assert.Equal(commerceId, repository.ReceivedCommerceId);
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.Equal("commerce01", result.AssociatedUser?.UserName);
    }

    [Fact]
    public async Task Missing_commerce_returns_null()
    {
        var commerceId = Guid.NewGuid();
        var repository = new CommerceRepositoryStub();
        var handler = new GetCommerceDetailQueryHandler(repository, CreateMapper());

        var result = await handler.Handle(new GetCommerceDetailQuery(commerceId), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(commerceId, repository.ReceivedCommerceId);
    }

    private static CommerceDetailReadModel CreateDetail(Guid commerceId) => new(
        commerceId, "Tienda Demo", null, "contacto@tiendademo.com", "8095551234",
        "101999999", CommerceStatus.Active, DateTimeOffset.UtcNow,
        new AssociatedCommerceUserReadModel("user-1", "commerce01", "commerce01@artemis.com", true));

    private static IMapper CreateMapper() => new MapperConfiguration(
        configuration => configuration.AddProfile<CommerceProfile>(),
        NullLoggerFactory.Instance).CreateMapper();
}
