using ABP.Application.Common;

namespace ABP.Application.Features.Loans
{
    public static class LoanErrors
    {
        public static readonly Error ClientNotFound = new(
            "Loans.ClientNotFound",
            "El cliente seleccionado no existe.");

        public static readonly Error ClientInactive = new(
            "Loans.ClientInactive",
            "El cliente seleccionado debe estar activo.");

        public static readonly Error ActiveLoanExists = new(
            "Loans.ActiveLoanExists",
            "El cliente seleccionado ya posee un préstamo activo.");

        public static readonly Error NotFound = new(
            "Loans.NotFound",
            "El préstamo especificado no existe.");

        public static readonly Error Inactive = new(
            "Loans.Inactive",
            "El préstamo debe estar activo para realizar esta operación.");

        public static readonly Error HighRiskConfirmationRequired = new(
            "Loans.HighRiskConfirmationRequired",
            "La asignación del préstamo requiere confirmación por el nivel de riesgo del cliente.");

        public static readonly Error NoFuturePendingInstallments = new(
            "Loans.NoFuturePendingInstallments",
            "El préstamo no posee cuotas futuras pendientes que puedan recalcularse.");

        public static readonly Error NoOutstandingBalance = new(
            "Loans.NoOutstandingBalance",
            "El préstamo no posee un balance pendiente para pagar.");

        public static readonly Error NumberGenerationFailed = new(
            "Loans.NumberGenerationFailed",
            "No fue posible generar un número de préstamo único.");

        public static readonly Error ConcurrencyConflict = new(
            "Loans.ConcurrencyConflict",
            "El préstamo fue modificado por otra operación. Intente nuevamente.");
    }
}
