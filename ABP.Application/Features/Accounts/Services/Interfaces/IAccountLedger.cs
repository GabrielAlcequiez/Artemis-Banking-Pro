using ABP.Domain.Enums;

namespace ABP.Application.Features.Accounts.Services.Interfaces;


public interface IAccountLedger
{
    Task RecordApprovedAsync( Guid operationId, Guid accountId,decimal amount,TransactionDirection direction,FinancialOperationType operationType,string? origin,
        string? beneficiary,string? actorUserId,string? actorRole,CancellationToken cancellationToken = default);

    Task RecordRejectedAsync(Guid accountId,Guid operationId,decimal amount,TransactionDirection direction,FinancialOperationType operationType,string rejectionReason,
        string? actorUserId,string? actorRole,CancellationToken cancellationToken = default);
}
