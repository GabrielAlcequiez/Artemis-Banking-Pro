using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Validation;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Features.Loans.Validation;

public sealed class LoanListRequestValidatorTests
{
    private readonly LoanListRequestValidator validator = new();

    [Fact]
    public void Default_request_is_valid()
    {
        var request = new LoanListRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
        Assert.Null(request.Identification);
        Assert.Null(request.Status);
        Assert.True(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(LoanStatusFilter.Active)]
    [InlineData(LoanStatusFilter.Completed)]
    [InlineData(LoanStatusFilter.All)]
    public void Every_defined_status_is_valid(LoanStatusFilter status)
    {
        var request = new LoanListRequest(Status: status);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_must_be_positive(int page)
    {
        var result = validator.Validate(new LoanListRequest(Page: page));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanListRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Page_size_must_be_between_one_and_twenty(int pageSize)
    {
        var result = validator.Validate(new LoanListRequest(PageSize: pageSize));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanListRequest.PageSize));
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData(" 123456789012 ")]
    public void Identification_cannot_exceed_eleven_trimmed_characters(
        string identification)
    {
        var result = validator.Validate(
            new LoanListRequest(Identification: identification));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanListRequest.Identification));
    }

    [Fact]
    public void Undefined_status_is_invalid()
    {
        var result = validator.Validate(
            new LoanListRequest(Status: (LoanStatusFilter)99));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(LoanListRequest.Status));
    }
}
