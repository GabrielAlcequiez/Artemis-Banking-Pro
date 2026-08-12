using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;

namespace ABP.Application.UnitTests.Features.Accounts.Validation
{
    public sealed class WithdrawalRequestValidatorTests
    {
        private readonly WithdrawalRequestValidator _validator = new();

        private static WithdrawalRequest ValidRequest() => new()
        {
            SourceAccountNumber = "100000001",
            Amount = 50m,
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
        public void Source_account_number_must_be_nine_digits(string accountNumber)
        {
            var request = ValidRequest();
            request.SourceAccountNumber = accountNumber;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(WithdrawalRequest.SourceAccountNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Amount_must_be_positive(decimal amount)
        {
            var request = ValidRequest();
            request.Amount = amount;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(WithdrawalRequest.Amount));
        }
    }
}
