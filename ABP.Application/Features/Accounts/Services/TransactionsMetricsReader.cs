using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Accounts.Services
{
    public sealed class TransactionsMetricsReader : ITransactionsMetricsReader
    {
        private readonly IAccountTransactionRepository _transactions;
        private readonly IClock _clock;

        public TransactionsMetricsReader(IAccountTransactionRepository transactions, IClock clock)
        {
            _transactions = transactions;
            _clock = clock;
        }

        public Task<int> CountTodayByActorAsync(string actorUserId, CancellationToken cancellationToken = default)
        {
            return _transactions.CountByActorTodayAsync(actorUserId, _clock.Today, cancellationToken);
        }

        public Task<decimal> SumTodayAmountByActorAsync(string actorUserId, CancellationToken cancellationToken = default)
        {
            return _transactions.SumAmountByActorTodayAsync(actorUserId, _clock.Today, cancellationToken);
        }
    }
}
