using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Features.Commerce.Validation;

public sealed class CommerceListRequestValidatorTests
{
    private readonly CommerceListRequestValidator _validator = new();

    [Fact]
    public void Default_request_is_valid()
    {
        var request = new CommerceListRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
        Assert.Null(request.Status);
        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(CommerceStatusFilter.Active)]
    [InlineData(CommerceStatusFilter.Inactive)]
    [InlineData(CommerceStatusFilter.All)]
    public void Every_defined_status_is_valid(CommerceStatusFilter status)
    {
        Assert.True(_validator.Validate(new CommerceListRequest(Status: status)).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_must_be_positive(int page)
    {
        var request = new CommerceListRequest(Page: page);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Page_size_must_be_between_one_and_twenty(int pageSize)
    {
        var request = new CommerceListRequest(PageSize: pageSize);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.PageSize));
    }

    [Fact]
    public void Undefined_status_is_invalid()
    {
        var request = new CommerceListRequest(Status: (CommerceStatusFilter)99);
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Status));
    }
}
