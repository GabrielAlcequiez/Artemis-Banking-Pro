using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;

namespace ABP.Application.UnitTests.Features.Accounts.Validation
{
    public sealed class DepositRequestValidatorTests
    {
        private readonly DepositRequestValidator _validator = new();

        private static DepositRequest ValidRequest() => new()
        {
            DestinationAccountNumber = "100000001",
            Amount = 100m,
            ActorUserId = "cashier-1",
            ActorRole = "Cashier"
        };

        [Fact]
        public void Valid_request_is_accepted()
        {
            var result = _validator.Validate(ValidRequest());
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("12345")]
        [InlineData("1234567890")]
        public void Destination_account_number_must_be_nine_digits(string accountNumber)
        {
            var request = ValidRequest();
            request.DestinationAccountNumber = accountNumber;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(DepositRequest.DestinationAccountNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Amount_must_be_positive(decimal amount)
        {
            var request = ValidRequest();
            request.Amount = amount;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(DepositRequest.Amount));
        }
    }
}
