using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Mapping;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.Commerce;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Commerce.Mapping;

public sealed class CommerceProfileTests
{
    private readonly MapperConfiguration _configuration = new(
        configuration => configuration.AddProfile<CommerceProfile>(),
        NullLoggerFactory.Instance);

    [Fact]
    public void Commerce_profile_configuration_is_valid()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(CommerceStatus.Active, true)]
    [InlineData(CommerceStatus.Inactive, false)]
    public void Summary_maps_domain_status_to_is_active(
        CommerceStatus status,
        bool expectedIsActive)
    {
        var source = new CommerceSummaryReadModel(
            Guid.NewGuid(),
            "Tienda Demo",
            null,
            "contacto@tiendademo.com",
            "8095551234",
            "101999999",
            status,
            true,
            DateTimeOffset.UtcNow);

        var result = _configuration.CreateMapper().Map<CommerceSummaryDto>(source);

        Assert.Equal(expectedIsActive, result.IsActive);
        Assert.True(result.HasAssociatedUser);
    }

    [Fact]
    public void Detail_maps_the_associated_user()
    {
        var user = new AssociatedCommerceUserReadModel(
            "user-1",
            "commerce01",
            "commerce01@artemis.com",
            true);
        var source = new CommerceDetailReadModel(
            Guid.NewGuid(),
            "Tienda Demo",
            "Comercio de prueba",
            "contacto@tiendademo.com",
            "8095551234",
            "101999999",
            CommerceStatus.Active,
            DateTimeOffset.UtcNow,
            user);

        var result = _configuration.CreateMapper().Map<CommerceDetailDto>(source);

        Assert.True(result.IsActive);
        Assert.NotNull(result.AssociatedUser);
        Assert.Equal(user.Id, result.AssociatedUser.Id);
        Assert.Equal(user.UserName, result.AssociatedUser.UserName);
        Assert.Equal(user.Email, result.AssociatedUser.Email);
        Assert.Equal(user.IsActive, result.AssociatedUser.IsActive);
    }
}
