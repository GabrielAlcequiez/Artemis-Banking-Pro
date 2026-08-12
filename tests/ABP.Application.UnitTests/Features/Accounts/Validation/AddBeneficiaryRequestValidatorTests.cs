using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;

namespace ABP.Application.UnitTests.Features.Accounts.Validation
{
    public sealed class AddBeneficiaryRequestValidatorTests
    {
        private readonly AddBeneficiaryRequestValidator _validator = new();

        private static AddBeneficiaryRequest ValidRequest() => new()
        {
            OwnerUserId = "user-1",
            BeneficiaryAccountNumber = "100000002"
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
        public void Beneficiary_account_number_must_be_nine_digits(string accountNumber)
        {
            var request = ValidRequest();
            request.BeneficiaryAccountNumber = accountNumber;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(AddBeneficiaryRequest.BeneficiaryAccountNumber));
        }
    }
}
