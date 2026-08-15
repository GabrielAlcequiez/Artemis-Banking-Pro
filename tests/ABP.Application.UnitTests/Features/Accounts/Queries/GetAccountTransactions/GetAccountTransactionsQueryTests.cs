using ABP.Application.Features.Accounts.Queries.GetAccountTransactions;
using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Accounts.Queries.GetAccountTransactions
{
    public sealed class GetAccountTransactionsQueryTests
    {
        [Fact]
        public void Account_id_is_required()
        {
            var validator = new GetAccountTransactionsQueryValidator();
            var query = new GetAccountTransactionsQuery(Guid.Empty, new PagedRequest(1, 10));

            var result = validator.Validate(query);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(GetAccountTransactionsQuery.AccountId));
        }

        [Fact]
        public async Task Handler_maps_the_page_of_transactions()
        {
            var accountId = Guid.NewGuid();
            var transaction = new AccountTransaction(Guid.NewGuid())
            {
                AccountId = accountId,
                OperationId = Guid.NewGuid(),
                Amount = 30m,
                Direction = TransactionDirection.Debit,
                OperationType = FinancialOperationType.Withdrawal,
                Status = TransactionStatus.Approved
            };
            var repository = new StubAccountTransactionRepository
            {
                Page = new PagedResult<AccountTransaction>([transaction], 1, 10, 1)
            };
            var handler = new GetAccountTransactionsQueryHandler(repository, CreateMapper());

            var result = await handler.Handle(
                new GetAccountTransactionsQuery(accountId, new PagedRequest(1, 10)), CancellationToken.None);

            Assert.Equal(1, result.TotalRecords);
            Assert.Single(result.Data);
            Assert.Equal(30m, result.Data.First().Amount);
        }

        private static IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddApplicationServices();

            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private sealed class StubAccountTransactionRepository : IAccountTransactionRepository
        {
            public PagedResult<AccountTransaction> Page { get; set; } = new([], 1, 10, 0);

            public Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync(
                Guid accountId, PagedRequest request, CancellationToken cancellationToken = default) =>
                Task.FromResult(Page);

            public Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync(
                Guid operationId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync(
                Guid accountId, int count, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyCollection<AccountTransaction>> GetAllByAccountAsync(
                Guid accountId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<int> CountByActorTodayAsync(string actorUserId, DateOnly today, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<decimal> SumAmountByActorTodayAsync(string actorUserId, DateOnly today, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<int> CountAllAsync(CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<int> CountByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<int> CountPaymentsAsync(CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<int> CountPaymentsByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public IQueryable<AccountTransaction> GetAllQueryable(bool trackChanges = false) =>
                throw new NotImplementedException();

            public Task<AccountTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyList<AccountTransaction>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<AccountTransaction> AddAsync(AccountTransaction entity, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<AccountTransaction?> UpdateAsync(Guid id, AccountTransaction value, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<AccountTransaction?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
        }
    }
}
