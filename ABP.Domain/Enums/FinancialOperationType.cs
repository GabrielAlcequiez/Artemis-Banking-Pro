namespace ABP.Domain.Enums
{
    public enum FinancialOperationType
    {
        InitialBalance = 1,
        AdministrativeCredit = 2,
        LoanDisbursement = 3,
        Deposit = 4,
        Withdrawal = 5,
        ExpressTransfer = 6,
        BeneficiaryTransfer = 7,
        OwnAccountTransfer = 8,
        ThirdPartyTransfer = 9,
        CreditCardPayment = 10,
        LoanPayment = 11,
        CashAdvance = 12,
        HermesPayment = 13,
        AccountCancellationTransfer = 14
    }
}