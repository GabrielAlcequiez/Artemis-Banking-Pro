using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeAccountBalanceService : IAccountBalanceService
    {
        public OperationResult DefaultCreditResult { get; set; } = OperationResult.Success();

        public OperationResult DefaultDebitResult { get; set; } = OperationResult.Success();

        private readonly Dictionary<Guid, OperationResult> _creditResults = new();
        private readonly Dictionary<Guid, OperationResult> _debitResults = new();

        public void SetCreditResultForAccount(Guid accountId, OperationResult result)
        {
            _creditResults[accountId] = result;
        }

        public void SetDebitResultForAccount(Guid accountId, OperationResult result)
        {
            _debitResults[accountId] = result;
        }

        public Task<OperationResult> CreditAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            var result = _creditResults.TryGetValue(accountId, out var configured) ? configured : DefaultCreditResult;
            return Task.FromResult(result);
        }

        public Task<OperationResult> DebitAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            var result = _debitResults.TryGetValue(accountId, out var configured) ? configured : DefaultDebitResult;
            return Task.FromResult(result);
        }
    }
}
