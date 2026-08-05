using ABP.Domain.Enums;

namespace ABP.Application.DTOs.Account;

public class AccountTransactionDto
{
    public required Guid Id { get; set; }
    public required Guid AccountId { get; set; }
    public required Guid OperationId { get; set; }
    public required decimal Amount { get; set; }
    public required TransactionDirection Direction { get; set; }
    public required FinancialOperationType OperationType { get; set; }
    public string? Origin { get; set; }
    public string? Beneficiary { get; set; }
    public required TransactionStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
}
