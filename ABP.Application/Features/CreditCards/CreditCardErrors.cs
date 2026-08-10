using ABP.Application.Common;

namespace ABP.Application.Features.CreditCards
{
    public static class CreditCardErrors
    {
        public static readonly Error ClientNotEligible = new(
            "CreditCards.ClientNotEligible",
            "The selected client does not exist or is not active.");

        public static readonly Error ClientNotFound = new(
            "CreditCards.ClientNotFound",
            "The selected client does not exist.");

        public static readonly Error ClientInactive = new(
            "CreditCards.ClientInactive",
            "The selected client is not active.");

        public static readonly Error NotFound = new(
            "CreditCards.NotFound",
            "The selected credit card does not exist.");

        public static readonly Error Cancelled = new(
            "CreditCards.Cancelled",
            "A cancelled credit card cannot be modified.");

        public static readonly Error LimitBelowDebt = new(
            "CreditCards.LimitBelowDebt",
            "The credit limit cannot be lower than the current debt.");

        public static readonly Error OutstandingDebt = new(
            "CreditCards.OutstandingDebt",
            "The credit card cannot be cancelled while it has outstanding debt.");

        public static readonly Error NumberGenerationFailed = new(
            "CreditCards.NumberGenerationFailed",
            "A unique credit card number could not be generated.");

        public static readonly Error AdministratorRequired = new(
            "CreditCards.AdministratorRequired",
            "An authenticated administrator is required to assign a credit card.");
    }
}
