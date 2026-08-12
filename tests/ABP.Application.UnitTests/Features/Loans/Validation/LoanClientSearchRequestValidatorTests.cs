using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Validation;

public sealed class LoanClientSearchRequestValidatorTests
{
    private readonly LoanClientSearchRequestValidator validator = new();

    [Fact]
    public void Default_request_is_valid()
    {
        var result = validator.Validate(new LoanClientSearchRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_must_be_positive(int page)
    {
        var result = validator.Validate(
            new LoanClientSearchRequest(Page: page));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanClientSearchRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Page_size_must_be_between_one_and_twenty(int pageSize)
    {
        var result = validator.Validate(
            new LoanClientSearchRequest(PageSize: pageSize));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanClientSearchRequest.PageSize));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901")]
    [InlineData(" 12345678901 ")]
    public void Identification_is_optional_and_accepts_eleven_trimmed_characters(
        string? identification)
    {
        var result = validator.Validate(
            new LoanClientSearchRequest(Identification: identification));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData(" 123456789012 ")]
    public void Identification_cannot_exceed_eleven_trimmed_characters(
        string identification)
    {
        var result = validator.Validate(
            new LoanClientSearchRequest(Identification: identification));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanClientSearchRequest.Identification));
    }
}
