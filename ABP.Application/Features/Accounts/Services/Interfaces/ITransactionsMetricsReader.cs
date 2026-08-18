using ABP.Application.Features.Accounts.DTOs;

namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface ITransactionsMetricsReader
{
    Task<int> CountTodayByActorAsync(string actorUserId, CancellationToken cancellationToken = default);

    Task<decimal> SumTodayAmountByActorAsync(string actorUserId, CancellationToken cancellationToken = default);

    /// <summary>Home indicators for a Cashier: today's total transactions, payments, deposits and withdrawals they performed.</summary>
    Task<CashierDailyOperationsSummaryDto> GetCashierDailySummaryAsync(
        string actorUserId, CancellationToken cancellationToken = default);
}
