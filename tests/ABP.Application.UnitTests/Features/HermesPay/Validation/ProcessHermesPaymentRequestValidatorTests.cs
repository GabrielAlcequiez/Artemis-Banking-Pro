using ABP.Application.Features.HermesPay.DTOs;
using ABP.Application.Features.HermesPay.Validation;

namespace ABP.Application.UnitTests.Features.HermesPay.Validation;

public sealed class ProcessHermesPaymentRequestValidatorTests
{
    private readonly ProcessHermesPaymentRequestValidator _validator = new();

    [Fact]
    public void Valid_request_with_two_decimal_places_is_accepted()
    {
        var result = _validator.Validate(CreateValidRequest(transactionAmount: 250.25m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Card_number_is_required_and_must_have_sixteen_digits()
    {
        var result = _validator.Validate(CreateValidRequest(cardNumber: string.Empty));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(ProcessHermesPaymentRequest.CardNumber)
            && error.ErrorMessage == "El número de tarjeta debe contener exactamente 16 dígitos.");
    }

    [Fact]
    public void Transaction_amount_rejects_more_than_two_decimal_places()
    {
        var result = _validator.Validate(
            CreateValidRequest(transactionAmount: 250.001m));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(ProcessHermesPaymentRequest.TransactionAmount)
            && error.ErrorMessage == "El monto de la transacción debe tener un máximo de dos decimales.");
    }

    [Fact]
    public void Operation_id_is_required()
    {
        var result = _validator.Validate(CreateValidRequest(operationId: Guid.Empty));

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(ProcessHermesPaymentRequest.OperationId)
            && error.ErrorMessage == "El encabezado Idempotency-Key es requerido y debe ser un GUID válido.");
    }

    private static ProcessHermesPaymentRequest CreateValidRequest(
        decimal transactionAmount = 250m,
        Guid? operationId = null,
        string cardNumber = "1589963258467598") =>
        new(
            requestedCommerceId: Guid.NewGuid(),
            cardNumber: cardNumber,
            expirationMonth: 8,
            expirationYear: 2029,
            cvc: "123",
            transactionAmount: transactionAmount,
            operationId: operationId ?? Guid.NewGuid());
}
