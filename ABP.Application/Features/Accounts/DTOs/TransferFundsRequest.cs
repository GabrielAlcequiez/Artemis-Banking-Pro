using ABP.Domain.Enums;

namespace ABP.Application.Features.Accounts.DTOs;

/// <summary>
/// Covers the three transfer flavors a Client can perform; <see cref="OperationType"/>
/// must be one of <see cref="FinancialOperationType.ExpressTransfer"/>,
/// <see cref="FinancialOperationType.BeneficiaryTransfer"/> or
/// <see cref="FinancialOperationType.OwnAccountTransfer"/>.
/// </summary>
public class TransferFundsRequest
{
    public required Guid SourceAccountId { get; set; }
    public string? DestinationAccountNumber { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public required decimal Amount { get; set; }
    public required FinancialOperationType OperationType { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorRole { get; set; }
}
