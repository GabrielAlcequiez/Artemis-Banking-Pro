using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Accounts.Services
{
    public sealed class TransactionsMetricsReader : ITransactionsMetricsReader
    {
        private static readonly FinancialOperationType[] PaymentTypes =
        [
            FinancialOperationType.CreditCardPayment,
            FinancialOperationType.LoanPayment
        ];

        private static readonly FinancialOperationType[] ThirdPartyTransferTypes =
        [
            FinancialOperationType.ExpressTransfer
        ];

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

        public async Task<CashierDailyOperationsSummaryDto> GetCashierDailySummaryAsync(
            string actorUserId, CancellationToken cancellationToken = default)
        {
            var today = _clock.Today;

            var deposits = await _transactions.CountByActorAndTypesTodayAsync(
                actorUserId, today, [FinancialOperationType.Deposit],
                cancellationToken: cancellationToken);

            var withdrawals = await _transactions.CountByActorAndTypesTodayAsync(
                actorUserId, today, [FinancialOperationType.Withdrawal],
                cancellationToken: cancellationToken);

            var payments = await _transactions.CountByActorAndTypesTodayAsync(
                actorUserId, today, PaymentTypes,
                cancellationToken: cancellationToken);

            var thirdPartyTransfers = await _transactions.CountByActorAndTypesTodayAsync(
                actorUserId, today, ThirdPartyTransferTypes,
                direction: TransactionDirection.Debit,
                cancellationToken: cancellationToken);

            var totalTransactions = deposits + withdrawals + payments + thirdPartyTransfers;

            return new CashierDailyOperationsSummaryDto(
                totalTransactions, payments, deposits, withdrawals);
        }
    }
}
