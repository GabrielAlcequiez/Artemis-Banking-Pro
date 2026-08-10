namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface ITransactionsMetricsReader
{
    Task<int> CountTodayByActorAsync(string actorUserId, CancellationToken cancellationToken = default);

    Task<decimal> SumTodayAmountByActorAsync(string actorUserId, CancellationToken cancellationToken = default);
}
