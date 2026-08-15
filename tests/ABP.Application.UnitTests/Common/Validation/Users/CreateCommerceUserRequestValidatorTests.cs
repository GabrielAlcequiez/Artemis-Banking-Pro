using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Validation.Users;

namespace ABP.Application.UnitTests.Common.Validation.Users;

public sealed class CreateCommerceUserRequestValidatorTests
{
    private readonly CreateCommerceUserRequestValidator _validator = new();

    [Fact]
    public async Task Valid_request_matches_user_persistence_limits()
    {
        var result = await _validator.ValidateAsync(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(nameof(CreateCommerceUserRequestDto.FirstName), 51)]
    [InlineData(nameof(CreateCommerceUserRequestDto.LastName), 51)]
    [InlineData(nameof(CreateCommerceUserRequestDto.Identification), 12)]
    [InlineData(nameof(CreateCommerceUserRequestDto.Email), 257)]
    [InlineData(nameof(CreateCommerceUserRequestDto.UserName), 21)]
    public async Task Value_longer_than_database_limit_is_rejected(
        string propertyName,
        int length)
    {
        var request = ValidRequest();
        var value = new string('a', length);

        switch (propertyName)
        {
            case nameof(CreateCommerceUserRequestDto.FirstName):
                request.FirstName = value;
                break;
            case nameof(CreateCommerceUserRequestDto.LastName):
                request.LastName = value;
                break;
            case nameof(CreateCommerceUserRequestDto.Identification):
                request.Identification = value;
                break;
            case nameof(CreateCommerceUserRequestDto.Email):
                request.Email = $"{new string('a', length - 9)}@test.com";
                break;
            case nameof(CreateCommerceUserRequestDto.UserName):
                request.UserName = value;
                break;
        }

        var result = await _validator.ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }

    private static CreateCommerceUserRequestDto ValidRequest() => new()
    {
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana@example.test",
        UserName = "ana-commerce",
        Password = "Passw0rd!",
        ConfirmPassword = "Passw0rd!",
        InitialAmount = 100m
    };
}
