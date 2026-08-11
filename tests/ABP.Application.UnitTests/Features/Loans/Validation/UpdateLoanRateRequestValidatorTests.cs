using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Validation
{
    public sealed class UpdateLoanRateRequestValidatorTests
    {
        private readonly UpdateLoanRateRequestValidator _validator = new();

        [Fact]
        public void Valid_request_is_accepted()
        {
            var request = new UpdateLoanRateRequest(Guid.NewGuid(), 10.5m);

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Loan_id_is_required()
        {
            var request = new UpdateLoanRateRequest(Guid.Empty, 10.5m);

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateLoanRateRequest.LoanId)
                && error.ErrorMessage == "El identificador del préstamo es obligatorio.");
        }

        [Fact]
        public void Annual_interest_rate_cannot_be_negative()
        {
            var request = new UpdateLoanRateRequest(Guid.NewGuid(), -0.01m);

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateLoanRateRequest.AnnualInterestRate)
                && error.ErrorMessage == "La tasa de interés anual no puede ser negativa.");
        }

        [Fact]
        public void Annual_interest_rate_accepts_at_most_four_decimal_places()
        {
            var request = new UpdateLoanRateRequest(Guid.NewGuid(), 10.12345m);

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateLoanRateRequest.AnnualInterestRate)
                && error.ErrorMessage == "La tasa de interés anual debe tener un máximo de cuatro decimales.");
        }
    }
}
