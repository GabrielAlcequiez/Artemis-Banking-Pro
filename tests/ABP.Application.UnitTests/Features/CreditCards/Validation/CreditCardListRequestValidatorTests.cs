using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Features.CreditCards.Validation;

public sealed class CreditCardListRequestValidatorTests
{
    private readonly CreditCardListRequestValidator _validator = new();

    [Fact]
    public void Default_request_is_valid()
    {
        var request = new CreditCardListRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
        Assert.Null(request.Identification);
        Assert.Null(request.Status);
        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Page_size_boundaries_are_valid(int pageSize)
    {
        var request = new CreditCardListRequest(
            Page: 1,
            PageSize: pageSize,
            Identification: " 12345678901 ",
            Status: CreditCardStatusFilter.Active);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(CreditCardStatusFilter.Active)]
    [InlineData(CreditCardStatusFilter.Cancelled)]
    [InlineData(CreditCardStatusFilter.All)]
    public void Every_defined_status_is_valid(CreditCardStatusFilter status)
    {
        var request = new CreditCardListRequest(Status: status);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_must_be_positive(int page)
    {
        var request = new CreditCardListRequest(Page: page);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardListRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Page_size_must_be_between_one_and_twenty(int pageSize)
    {
        var request = new CreditCardListRequest(PageSize: pageSize);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardListRequest.PageSize));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901")]
    [InlineData(" 12345678901 ")]
    [InlineData("ABC-123")]
    public void Identification_is_optional_and_does_not_require_numeric_format(string? identification)
    {
        var request = new CreditCardListRequest(Identification: identification);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData(" 123456789012 ")]
    public void Identification_cannot_exceed_eleven_trimmed_characters(string identification)
    {
        var request = new CreditCardListRequest(Identification: identification);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardListRequest.Identification));
    }

    [Fact]
    public void Undefined_status_is_invalid()
    {
        var request = new CreditCardListRequest(Status: (CreditCardStatusFilter)99);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardListRequest.Status));
    }
}
