using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Validation;

namespace ABP.Application.UnitTests.Features.CreditCards.Validation
{
    public sealed class CancelCreditCardRequestValidatorTests
    {
        private readonly CancelCreditCardRequestValidator _validator = new();

        [Fact]
        public void Valid_request_is_accepted()
        {
            var request = new CancelCreditCardRequest(
                CreditCardId: Guid.NewGuid());
            // When
            var result = _validator.Validate(request);
            // Then
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Credit_card_id_is_required()
        {
            var request = new CancelCreditCardRequest(CreditCardId: Guid.Empty);
            var result = _validator.Validate(request);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CancelCreditCardRequest.CreditCardId));
        }

    }
}
