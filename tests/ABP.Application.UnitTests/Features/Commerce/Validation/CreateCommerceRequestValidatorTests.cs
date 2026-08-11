using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;

namespace ABP.Application.UnitTests.Features.Commerce.Validation;

public sealed class CreateCommerceRequestValidatorTests
{
    private readonly CreateCommerceRequestValidator _validator = new();

    [Fact]
    public void Valid_request_is_accepted()
    {
        var request = ValidRequest();

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Required_fields_reject_whitespace()
    {
        var request = new CreateCommerceRequest("   ", null, "   ", "   ", "   ");
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.PhoneNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Rnc));
    }

    [Fact]
    public void Email_must_have_a_valid_format()
    {
        var request = ValidRequest() with { Email = "correo-invalido" };
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Email));
    }

    [Fact]
    public void Persistence_length_limits_are_enforced_before_saving()
    {
        var request = new CreateCommerceRequest(
            new string('N', 151),
            new string('D', 501),
            $"{new string('e', 250)}@mail.com",
            new string('1', 21),
            new string('2', 12));
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Description));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.PhoneNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Rnc));
    }

    private static CreateCommerceRequest ValidRequest() => new(
        "Tienda Demo",
        "Comercio de prueba",
        "contacto@tiendademo.com",
        "8095551234",
        "101999999");
}
