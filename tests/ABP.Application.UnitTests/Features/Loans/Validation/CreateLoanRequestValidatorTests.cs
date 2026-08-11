using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Validation
{
    public sealed class CreateLoanRequestValidatorTests
    {
        private readonly CreateLoanRequestValidator _validator = new();

        [Fact]
        public void Valid_request_is_accepted()
        {
            // Given
            var request = CreateValidRequest();

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
            var request = CreateValidRequest() with { ClientId = clientId };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateLoanRequest.ClientId)
                && error.ErrorMessage == "El identificador del cliente es obligatorio.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Capital_amount_must_be_positive(int capitalAmount)
        {
            // Arrange
            var request = CreateValidRequest() with { CapitalAmount = capitalAmount };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateLoanRequest.CapitalAmount)
                && error.ErrorMessage == "El monto del capital debe ser mayor que cero.");
        }

        [Fact]
        public void Capital_amount_accepts_at_most_two_decimal_places()
        {
            // Arrange
            var request = CreateValidRequest() with { CapitalAmount = 10_000.001m };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateLoanRequest.CapitalAmount)
                && error.ErrorMessage == "El monto del capital debe tener un máximo de dos decimales.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(66)]
        public void Term_must_be_an_allowed_value(int termInMonths)
        {
            // Arrange
            var request = CreateValidRequest() with { TermInMonths = termInMonths };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateLoanRequest.TermInMonths));
        }

        [Fact]
        public void Annual_interest_rate_cannot_be_negative()
        {
            // Arrange
            var request = CreateValidRequest() with { AnnualInterestRate = -0.01m };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateLoanRequest.AnnualInterestRate)
                && error.ErrorMessage == "La tasa de interés anual no puede ser negativa.");
        }

        [Fact]
        public void Annual_interest_rate_accepts_at_most_four_decimal_places()
        {
            // Arrange
            var request = CreateValidRequest() with { AnnualInterestRate = 12.34567m };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateLoanRequest.AnnualInterestRate)
                && error.ErrorMessage == "La tasa de interés anual debe tener un máximo de cuatro decimales.");
        }

        private static CreateLoanRequest CreateValidRequest() =>
            new(
                ClientId: "cliente-123",
                CapitalAmount: 100_000m,
                TermInMonths: 12,
                AnnualInterestRate: 12m);
    }
}
