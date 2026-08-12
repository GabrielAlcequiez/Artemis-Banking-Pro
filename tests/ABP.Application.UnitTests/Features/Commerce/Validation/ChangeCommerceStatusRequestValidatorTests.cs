using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;

namespace ABP.Application.UnitTests.Features.Commerce.Validation;

public sealed class ChangeCommerceStatusRequestValidatorTests
{
    private readonly ChangeCommerceStatusRequestValidator _validator = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Both_commerce_states_are_valid(bool isActive)
    {
        var request = new ChangeCommerceStatusRequest(Guid.NewGuid(), isActive);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Commerce_id_is_required()
    {
        var request = new ChangeCommerceStatusRequest(Guid.Empty, false);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.CommerceId));
    }
}
