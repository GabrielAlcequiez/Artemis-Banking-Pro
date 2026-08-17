using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Validation;

namespace ABP.Application.UnitTests.Features.CreditCards.Validation;

public sealed class CreditCardPaymentRequestValidatorTests
{
    private readonly CreditCardPaymentRequestValidator _validator = new();

    [Fact]
    public void Valid_request_with_two_decimal_places_is_accepted()
    {
        var result = _validator.Validate(CreateValidRequest() with { Amount = 1_000.25m });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Credit_card_id_is_required()
    {
        var result = _validator.Validate(CreateValidRequest() with { CreditCardId = Guid.Empty });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardPaymentRequest.CreditCardId)
            && error.ErrorMessage == "La tarjeta de crédito destino es requerida.");
    }

    [Fact]
    public void Source_account_id_is_required()
    {
        var result = _validator.Validate(CreateValidRequest() with { SourceAccountId = Guid.Empty });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardPaymentRequest.SourceAccountId)
            && error.ErrorMessage == "La cuenta de origen es requerida.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Amount_must_be_positive(int amount)
    {
        var result = _validator.Validate(CreateValidRequest() with { Amount = amount });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardPaymentRequest.Amount)
            && error.ErrorMessage == "El monto a pagar debe ser mayor que cero.");
    }

    [Fact]
    public void Amount_rejects_more_than_two_decimal_places()
    {
        var result = _validator.Validate(CreateValidRequest() with { Amount = 1_000.001m });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardPaymentRequest.Amount)
            && error.ErrorMessage == "El monto a pagar debe tener un máximo de dos decimales.");
    }

    [Fact]
    public void Operation_id_is_required()
    {
        var result = _validator.Validate(CreateValidRequest() with { OperationId = Guid.Empty });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreditCardPaymentRequest.OperationId)
            && error.ErrorMessage == "El identificador de la operación es requerido.");
    }

    private static CreditCardPaymentRequest CreateValidRequest() =>
        new(
            CreditCardId: Guid.NewGuid(),
            SourceAccountId: Guid.NewGuid(),
            Amount: 1_000m,
            OperationId: Guid.NewGuid());
}
