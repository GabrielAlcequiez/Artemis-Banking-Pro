using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Validation;

namespace ABP.Application.UnitTests.Features.CreditCards.Validation
{
    public sealed class CreateCreditCardRequestValidatorTests
    {
        private readonly CreateCreditCardRequestValidator _validator = new();

        [Fact]
        public void Valid_request_is_accepted()
        {
            // Given
            var request = new CreateCreditCardRequest(
                ClientId: "cliente-123",
                CreditLimit: 10_000m);
            // When
            var result = _validator.Validate(request);
            // Then
            Assert.True(result.IsValid);
        }
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Client_id_is_required(string clientId)
        {
            // Arrange
            var request = new CreateCreditCardRequest(
                ClientId: clientId,
                CreditLimit: 10_000m);

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateCreditCardRequest.ClientId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Credit_limit_must_be_positive(int creditLimit)
        {
            // Arrange
            var request = new CreateCreditCardRequest(
                ClientId: "client-123",
                CreditLimit: creditLimit);

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateCreditCardRequest.CreditLimit));
        }
    }
}