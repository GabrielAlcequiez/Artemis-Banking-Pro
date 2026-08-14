using ABP.Application.Common;

namespace ABP.Application.Features.HermesPay;

public static class HermesPayErrors
{
    public static readonly Error AuthenticationRequired = new(
        "HermesPay.AuthenticationRequired",
        "Debe autenticarse para utilizar Hermes Pay.");

    public static readonly Error RoleNotAllowed = new(
        "HermesPay.RoleNotAllowed",
        "Solo los usuarios Administrador o Comercio pueden utilizar Hermes Pay.");

    public static readonly Error CommerceUserInactive = new(
        "HermesPay.CommerceUserInactive",
        "El usuario de comercio autenticado no se encuentra activo.");

    public static readonly Error CommerceAssociationRequired = new(
        "HermesPay.CommerceAssociationRequired",
        "El usuario de comercio autenticado no tiene un comercio asociado.");

    public static readonly Error CommerceNotFound = new(
        "HermesPay.CommerceNotFound",
        "El comercio indicado no existe.");

    public static readonly Error CommerceInactive = new(
        "HermesPay.CommerceInactive",
        "El comercio indicado no se encuentra activo.");

    public static readonly Error AssociatedCommerceUserRequired = new(
        "HermesPay.AssociatedCommerceUserRequired",
        "El comercio debe tener un usuario de comercio asociado.");

    public static readonly Error AssociatedCommerceUserInactive = new(
        "HermesPay.AssociatedCommerceUserInactive",
        "El usuario asociado al comercio no se encuentra activo.");

    public static readonly Error PrimaryAccountRequired = new(
        "HermesPay.PrimaryAccountRequired",
        "El usuario asociado al comercio debe tener una cuenta de ahorro principal activa.");

    public static readonly Error CardNotFound = new(
        "HermesPay.CardNotFound",
        "Los datos de la tarjeta proporcionados no son válidos.");

    public static readonly Error CardInactive = new(
        "HermesPay.CardInactive",
        "La tarjeta indicada no se encuentra activa.");

    public static readonly Error CardExpired = new(
        "HermesPay.CardExpired",
        "La tarjeta indicada se encuentra vencida.");

    public static readonly Error CardDataMismatch = new(
        "HermesPay.CardDataMismatch",
        "Los datos proporcionados no coinciden con la tarjeta indicada.");

    public static readonly Error InsufficientCredit = new(
        "HermesPay.InsufficientCredit",
        "El monto de la transacción excede el crédito disponible de la tarjeta.");

    public static readonly Error OperationIdConflict = new(
        "HermesPay.OperationIdConflict",
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
                ? "HermesPay.OperationRejected"
                : code,
            string.IsNullOrWhiteSpace(description)
                ? "La operación de Hermes Pay fue rechazada."
                : description);
    }

    private static readonly Error[] All =
    [
        AuthenticationRequired,
        RoleNotAllowed,
        CommerceUserInactive,
        CommerceAssociationRequired,
        CommerceNotFound,
        CommerceInactive,
        AssociatedCommerceUserRequired,
        AssociatedCommerceUserInactive,
        PrimaryAccountRequired,
        CardNotFound,
        CardInactive,
        CardExpired,
        CardDataMismatch,
        InsufficientCredit,
        OperationIdConflict
    ];
}
