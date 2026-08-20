using ABP.Application.Common;

namespace ABP.Application.Features.Accounts
{
    public static class AccountErrors
    {
        public static readonly Error NotFound = new(
            "Accounts.NotFound",
            "La cuenta seleccionada no existe.");

        public static readonly Error InvalidAmount = new(
            "Accounts.InvalidAmount",
            "El monto debe ser mayor que cero.");

        public static readonly Error InsufficientFunds = new(
            "Accounts.InsufficientFunds",
            "La cuenta no tiene fondos suficientes para esta operación.");

        public static readonly Error InactiveAccount = new(
            "Accounts.InactiveAccount",
            "La cuenta no se encuentra activa.");

        public static readonly Error PrincipalAlreadyExists = new(
            "Accounts.PrincipalAlreadyExists",
            "El cliente ya tiene una cuenta de ahorro principal.");

        public static readonly Error SameAccount = new(
            "Accounts.SameAccount",
            "La cuenta de origen y destino deben ser diferentes.");

        public static readonly Error CannotAddSelf = new(
            "Accounts.CannotAddSelf",
            "No puedes agregar tu propia cuenta como beneficiario.");

        public static readonly Error BeneficiaryAlreadyExists = new(
            "Accounts.BeneficiaryAlreadyExists",
            "Esta cuenta ya está registrada como beneficiario.");

        public static readonly Error BeneficiaryNotFound = new(
            "Accounts.BeneficiaryNotFound",
            "El beneficiario indicado no existe.");

        public static readonly Error CannotCancelPrincipal = new(
            "Accounts.CannotCancelPrincipal",
            "La cuenta principal no se puede cancelar.");

        public static readonly Error AlreadyCancelled = new(
            "Accounts.AlreadyCancelled",
            "La cuenta ya se encuentra cancelada.");

        public static readonly Error PrincipalNotFound = new(
            "Accounts.PrincipalNotFound",
            "El saldo no pudo transferirse porque el cliente no tiene cuenta principal.");

        public static readonly Error NotEnoughActiveAccounts = new(
            "Accounts.NotEnoughActiveAccounts",
            "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.");
    }
}
