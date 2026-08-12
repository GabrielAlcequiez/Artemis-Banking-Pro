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

        public static readonly Error AdministratorRequired = new(
            "Loans.AdministratorRequired",
            "Se requiere un administrador autenticado para asignar un préstamo.");

        public static readonly Error PrincipalAccountNotFound = new(
            "Loans.PrincipalAccountNotFound",
            "El cliente debe tener una cuenta de ahorro principal activa para recibir el desembolso.");

        public static readonly Error PaymentActorRequired = new(
            "Loans.PaymentActorRequired",
            "Se requiere un cliente o cajero autenticado para procesar el pago.");

        public static readonly Error SourceAccountNotFound = new(
            "Loans.SourceAccountNotFound",
            "La cuenta de origen especificada no existe.");

        public static readonly Error LoanOwnershipRequired = new(
            "Loans.LoanOwnershipRequired",
            "El cliente autenticado solo puede pagar sus propios préstamos.");

        public static readonly Error AccountOwnershipRequired = new(
            "Loans.AccountOwnershipRequired",
            "El cliente autenticado solo puede pagar desde una cuenta propia.");

        public static readonly Error OperationConflict = new(
            "Loans.OperationConflict",
            "El identificador de operación ya fue utilizado para otro pago.");

        public static readonly Error ConcurrencyConflict = new(
            "Loans.ConcurrencyConflict",
            "El préstamo fue modificado por otra operación. Intente nuevamente.");
    }
}
