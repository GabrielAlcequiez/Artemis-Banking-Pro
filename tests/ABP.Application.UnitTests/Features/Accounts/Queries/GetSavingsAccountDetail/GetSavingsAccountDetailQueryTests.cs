using ABP.Application.Features.Accounts.Queries.GetSavingsAccountDetail;
using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Accounts.Queries.GetSavingsAccountDetail
{
    public sealed class GetSavingsAccountDetailQueryTests
    {
        [Fact]
        public async Task Handler_with_existing_account_maps_detail_and_recent_transactions()
        {
            var accountId = Guid.NewGuid();
            var account = new SavingsAccount(accountId)
            {
                OwnerUserId = "user-1",
                AccountNumber = "100000001",
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active,
                Balance = 250m
            };
            var transaction = new AccountTransaction(Guid.NewGuid())
            {
                AccountId = accountId,
                OperationId = Guid.NewGuid(),
                Amount = 50m,
                Direction = TransactionDirection.Credit,
                OperationType = FinancialOperationType.Deposit,
                Status = TransactionStatus.Approved
            };
            var accounts = new StubSavingsAccountRepository { Account = account };
            var transactions = new StubAccountTransactionRepository { RecentTransactions = [transaction] };
            var handler = new GetSavingsAccountDetailQueryHandler(accounts, transactions, CreateMapper());

            var result = await handler.Handle(new GetSavingsAccountDetailQuery(accountId), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("100000001", result.AccountNumber);
            Assert.Equal(250m, result.Balance);
            Assert.Single(result.RecentTransactions);
        }

        [Fact]
        public async Task Handler_with_missing_account_returns_null()
        {
            var accounts = new StubSavingsAccountRepository { Account = null };
            var transactions = new StubAccountTransactionRepository();
            var handler = new GetSavingsAccountDetailQueryHandler(accounts, transactions, CreateMapper());

            var result = await handler.Handle(new GetSavingsAccountDetailQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.Null(result);
        }

        private static IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddApplicationServices();

            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private sealed class StubSavingsAccountRepository : ISavingsAccountRepository
        {
            public SavingsAccount? Account { get; set; }

            public Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Account);

            public Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(
                string ownerUserId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<PagedResult<SavingsAccount>> GetPagedAsync(
                PagedRequest request, string? ownerIdentification = null, SavingsAccountStatus? status = null,
                SavingsAccountType? type = null, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) =>
                throw new NotImplementedException();

            public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount> AddAsync(SavingsAccount entity, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> UpdateAsync(Guid id, SavingsAccount value, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
        }

        private sealed class StubAccountTransactionRepository : IAccountTransactionRepository
        {
            public IReadOnlyCollection<AccountTransaction> RecentTransactions { get; set; } = [];

            public Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync(
                Guid accountId, int count, CancellationToken cancellationToken = default) =>
                Task.FromResult(RecentTransactions);

            public Task<IReadOnlyCollection<AccountTransaction>> GetAllByAccountAsync(
                Guid accountId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync(
                Guid accountId, PagedRequest request, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync(
                Guid operationId, CancellationToken cancellationToken = default) =>
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
