namespace ABP.Application.DTOs.Loan
{
    public sealed record LoanInstallmentDto(
        int InstallmentNumber,
        DateOnly DueDate,
        decimal InstallmentAmount,
        decimal InterestAmount,
        decimal CapitalAmount,
        decimal PendingInstallmentAmount,
        string PaymentStatus,
        bool IsLate);
}
