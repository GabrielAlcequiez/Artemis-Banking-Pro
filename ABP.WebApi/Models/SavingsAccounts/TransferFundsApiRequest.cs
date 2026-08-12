using ABP.Domain.Enums;

namespace ABP.WebApi.Models.SavingsAccounts;

public sealed class TransferFundsApiRequest
{
    public Guid SourceAccountId { get; set; }

    public string? DestinationAccountNumber { get; set; }

    public Guid? DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public FinancialOperationType OperationType { get; set; }
}
