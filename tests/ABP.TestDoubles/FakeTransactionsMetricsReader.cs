using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeTransactionsMetricsReader : ITransactionsMetricsReader
    {
        public int DefaultCount { get; set; } = 0;

        public decimal DefaultSum { get; set; } = 0m;

        public CashierDailyOperationsSummaryDto DefaultCashierSummary { get; set; } =
            new(0, 0, 0, 0);

        private readonly Dictionary<string, int> _countsByActor = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, decimal> _sumsByActor = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CashierDailyOperationsSummaryDto> _cashierSummariesByActor =
            new(StringComparer.OrdinalIgnoreCase);

        public void SetCountForActor(string actorUserId, int count)
        {
            _countsByActor[actorUserId] = count;
        }

        public void SetSumForActor(string actorUserId, decimal sum)
        {
            _sumsByActor[actorUserId] = sum;
        }

        public void SetCashierSummaryForActor(string actorUserId, CashierDailyOperationsSummaryDto summary)
        {
            _cashierSummariesByActor[actorUserId] = summary;
        }

        public Task<int> CountTodayByActorAsync(string actorUserId, CancellationToken cancellationToken = default)
        {
            var result = _countsByActor.TryGetValue(actorUserId, out var count) ? count : DefaultCount;
            return Task.FromResult(result);
        }

        public Task<decimal> SumTodayAmountByActorAsync(string actorUserId, CancellationToken cancellationToken = default)
        {
            var result = _sumsByActor.TryGetValue(actorUserId, out var sum) ? sum : DefaultSum;
            return Task.FromResult(result);
        }

        public Task<CashierDailyOperationsSummaryDto> GetCashierDailySummaryAsync(
            string actorUserId, CancellationToken cancellationToken = default)
        {
            var result = _cashierSummariesByActor.TryGetValue(actorUserId, out var summary)
                ? summary
                : DefaultCashierSummary;
            return Task.FromResult(result);
        }
    }
}
