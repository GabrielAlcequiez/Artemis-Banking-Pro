using ABP.Application.Common;

namespace ABP.Application.Features.Accounts
{
    public static class AccountErrors
    {
        public static readonly Error NotFound = new(
            "Accounts.NotFound",
            "The selected account does not exist.");

        public static readonly Error InvalidAmount = new(
            "Accounts.InvalidAmount",
            "The amount must be greater than zero.");

        public static readonly Error InsufficientFunds = new(
            "Accounts.InsufficientFunds",
            "The account has insufficient funds for this operation.");

        public static readonly Error InactiveAccount = new(
            "Accounts.InactiveAccount",
            "The account is not active.");

        public static readonly Error PrincipalAlreadyExists = new(
            "Accounts.PrincipalAlreadyExists",
            "The client already has a Principal savings account.");

        public static readonly Error SameAccount = new(
            "Accounts.SameAccount",
            "Source and destination accounts must be different.");

        public static readonly Error CannotAddSelf = new(
            "Accounts.CannotAddSelf",
            "You cannot add your own account as a beneficiary.");

        public static readonly Error BeneficiaryAlreadyExists = new(
            "Accounts.BeneficiaryAlreadyExists",
            "This account is already registered as a beneficiary.");

        public static readonly Error BeneficiaryNotFound = new(
            "Accounts.BeneficiaryNotFound",
            "The beneficiary was not found.");

        public static readonly Error CannotCancelPrincipal = new(
            "Accounts.CannotCancelPrincipal",
            "The Principal savings account cannot be cancelled.");

        public static readonly Error AlreadyCancelled = new(
            "Accounts.AlreadyCancelled",
            "The savings account is already cancelled.");

        public static readonly Error PrincipalNotFound = new(
            "Accounts.PrincipalNotFound",
            "The account balance cannot be transferred because the owner has no Principal account.");

        public static readonly Error NotEnoughActiveAccounts = new(
            "Accounts.NotEnoughActiveAccounts",
            "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.");
    }
}
