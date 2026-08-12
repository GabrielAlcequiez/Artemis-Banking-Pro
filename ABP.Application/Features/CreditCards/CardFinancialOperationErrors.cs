using ABP.Application.Common;

namespace ABP.Application.Features.CreditCards;

public static class CardFinancialOperationErrors
{
    public static readonly Error AuthenticationRequired = new(
        "CreditCards.AuthenticationRequired",
        "Debe iniciar sesión para realizar esta operación.");

    public static readonly Error RoleNotAllowed = new(
        "CreditCards.RoleNotAllowed",
        "Su rol no tiene permiso para realizar esta operación.");

    public static readonly Error CardNotFound = new(
        "CreditCards.CardNotFound",
        "La tarjeta seleccionada no existe.");

    public static readonly Error AccountNotFound = new(
        "CreditCards.AccountNotFound",
        "La cuenta de ahorro seleccionada no existe.");

    public static readonly Error OwnershipRequired = new(
        "CreditCards.OwnershipRequired",
        "Solo puede utilizar tarjetas y cuentas de ahorro que le pertenezcan.");

    public static readonly Error CardInactive = new(
        "CreditCards.CardInactive",
        "La tarjeta seleccionada no se encuentra activa.");

    public static readonly Error AccountInactive = new(
        "CreditCards.AccountInactive",
        "La cuenta de ahorro seleccionada no se encuentra activa.");

    public static readonly Error CardWithoutDebt = new(
        "CreditCards.CardWithoutDebt",
        "La tarjeta seleccionada no tiene deuda pendiente.");

    public static readonly Error InsufficientFunds = new(
        "CreditCards.InsufficientFunds",
        "No dispone del monto requerido en la cuenta seleccionada.");

    public static readonly Error CardExpired = new(
        "CreditCards.CardExpired",
        "La tarjeta seleccionada se encuentra vencida.");

    public static readonly Error InsufficientCredit = new(
        "CreditCards.InsufficientCredit",
        "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.");

    public static readonly Error OperationIdConflict = new(
        "CreditCards.OperationIdConflict",
        "El identificador de operación ya fue utilizado con datos diferentes.");

    public static Error ResolvePersisted(
        string? code,
        string? description)
    {
        var knownError = All.FirstOrDefault(error => error.Code == code);
        if (knownError is not null)
        {
            return knownError;
        }

        return new Error(
            string.IsNullOrWhiteSpace(code)
                ? "CreditCards.OperationRejected"
                : code,
            string.IsNullOrWhiteSpace(description)
                ? "La operación fue rechazada."
                : description);
    }

    private static readonly Error[] All =
    [
        AuthenticationRequired,
        RoleNotAllowed,
        CardNotFound,
        AccountNotFound,
        OwnershipRequired,
        CardInactive,
        AccountInactive,
        CardWithoutDebt,
        InsufficientFunds,
        CardExpired,
        InsufficientCredit,
        OperationIdConflict
    ];
}
