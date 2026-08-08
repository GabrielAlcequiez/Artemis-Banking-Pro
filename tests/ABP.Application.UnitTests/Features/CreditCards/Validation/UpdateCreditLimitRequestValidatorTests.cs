using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Validation;

namespace ABP.Application.UnitTests.Features.CreditCards.Validation
{
    public sealed class UpdateCreditLimitRequestValidatorTests
    {
        private readonly UpdateCreditLimitRequestValidator _validator = new();

        [Fact]
        public void Valid_request_is_accepted()
        {
            // Given
            var request = new UpdateCreditLimitRequest(
                CreditCardId: Guid.NewGuid(),
                CreditLimit: 10_000m);
            // When
            var result = _validator.Validate(request);
            // Then
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Credit_card_id_is_required()
        {
            var request = new UpdateCreditLimitRequest(
                CreditCardId: Guid.Empty,
                CreditLimit: 10_000m);

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateCreditLimitRequest.CreditCardId));
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Credit_limit_must_be_positive(int creditLimit)
        {
            var request = new UpdateCreditLimitRequest(
                CreditCardId: Guid.NewGuid(),
                CreditLimit: creditLimit);

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateCreditLimitRequest.CreditLimit));
        }

    }
}
