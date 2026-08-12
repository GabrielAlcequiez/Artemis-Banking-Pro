using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;

namespace ABP.Application.UnitTests.Features.Commerce.Validation;

public sealed class UpdateCommerceRequestValidatorTests
{
    private readonly UpdateCommerceRequestValidator _validator = new();

    [Fact]
    public void Valid_request_is_accepted()
    {
        var request = ValidRequest();

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Commerce_id_is_required()
    {
        var request = ValidRequest() with { CommerceId = Guid.Empty };
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.CommerceId));
    }

    [Fact]
    public void Update_reuses_the_commerce_data_rules()
    {
        var request = ValidRequest() with
        {
            Name = "   ",
            Email = "correo-invalido",
            PhoneNumber = "   ",
            Rnc = new string('1', 12)
        };
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.PhoneNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Rnc));
    }

    private static UpdateCommerceRequest ValidRequest() => new(
        Guid.NewGuid(),
        "Tienda Demo",
        "Comercio de prueba",
        "contacto@tiendademo.com",
        "8095551234",
        "101999999");
}
