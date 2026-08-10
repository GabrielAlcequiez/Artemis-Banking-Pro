using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Validation
{
    public sealed class LoanPaymentRequestValidatorTests
    {
        private readonly LoanPaymentRequestValidator _validator = new();

        [Fact]
        public void Valid_request_is_accepted()
        {
            var request = CreateValidRequest();

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Loan_id_is_required()
        {
            var request = CreateValidRequest() with { LoanId = Guid.Empty };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoanPaymentRequest.LoanId)
                && error.ErrorMessage == "El identificador del préstamo es obligatorio.");
        }

        [Fact]
        public void Source_account_id_is_required()
        {
            var request = CreateValidRequest() with { SourceAccountId = Guid.Empty };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoanPaymentRequest.SourceAccountId)
                && error.ErrorMessage == "El identificador de la cuenta de origen es obligatorio.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Amount_must_be_positive(int amount)
        {
            var request = CreateValidRequest() with { Amount = amount };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoanPaymentRequest.Amount)
                && error.ErrorMessage == "El monto del pago debe ser mayor que cero.");
        }

        [Fact]
        public void Amount_accepts_at_most_two_decimal_places()
        {
            var request = CreateValidRequest() with { Amount = 1_000.001m };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoanPaymentRequest.Amount)
                && error.ErrorMessage == "El monto del pago debe tener un máximo de dos decimales.");
        }

        [Fact]
        public void Operation_id_is_required()
        {
            var request = CreateValidRequest() with { OperationId = Guid.Empty };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoanPaymentRequest.OperationId)
                && error.ErrorMessage == "El identificador de la operación es obligatorio.");
        }

        private static LoanPaymentRequest CreateValidRequest() =>
            new(
                LoanId: Guid.NewGuid(),
                SourceAccountId: Guid.NewGuid(),
                Amount: 1_000m,
                OperationId: Guid.NewGuid());
    }
}
