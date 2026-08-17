using System.Data;
using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Accounts.Services;

public sealed class MoneyTransferServiceTests
{
    [Fact]
    public async Task TransferAsync_runs_the_movement_inside_a_financial_transaction()
    {
        var accounts = new FakeSavingsAccountRepository();
        var source = accounts.Seed(Guid.NewGuid(), SavingsAccountType.Principal, "100000001");
        var destination = accounts.Seed(Guid.NewGuid(), SavingsAccountType.Secondary, "100000002");
        var balances = new FakeAccountBalanceService();
        var ledger = new FakeAccountLedger();
        var transaction = new FakeFinancialTransaction();
        var service = CreateService(accounts, balances, ledger, transaction);

        var result = await service.TransferAsync(new TransferFundsRequest
        {
            SourceAccountId = source.Id,
            DestinationAccountId = destination.Id,
            Amount = 50m,
            OperationType = FinancialOperationType.ExpressTransfer,
            ActorUserId = "user-1",
            ActorRole = "Client"
        });

        Assert.True(result.IsSuccess);
        Assert.True(transaction.IsCommitted);
        Assert.Equal(2, ledger.RecordedApprovals.Count);
    }

    [Fact]
    public async Task TransferAsync_credit_failure_does_not_issue_a_compensating_credit()
    {
        var accounts = new FakeSavingsAccountRepository();
        var source = accounts.Seed(Guid.NewGuid(), SavingsAccountType.Principal, "100000001");
        var destination = accounts.Seed(Guid.NewGuid(), SavingsAccountType.Secondary, "100000002");
        var balances = new FakeAccountBalanceService();
        balances.SetCreditResultForAccount(destination.Id, OperationResult.Failure(AccountErrors.InsufficientFunds));
        var ledger = new FakeAccountLedger();
        var transaction = new FakeFinancialTransaction();
        var service = CreateService(accounts, balances, ledger, transaction);

        var result = await service.TransferAsync(new TransferFundsRequest
        {
            SourceAccountId = source.Id,
            DestinationAccountId = destination.Id,
            Amount = 50m,
            OperationType = FinancialOperationType.ExpressTransfer,
            ActorUserId = "user-1",
            ActorRole = "Client"
        });

        Assert.True(result.IsFailure);
        Assert.DoesNotContain(balances.Credits, credit => credit.AccountId == source.Id);
        Assert.Single(ledger.RecordedRejections);
        Assert.Empty(ledger.RecordedApprovals);
    }

    private static MoneyTransferService CreateService(
        FakeSavingsAccountRepository accounts,
        FakeAccountBalanceService balances,
        FakeAccountLedger ledger,
        FakeFinancialTransaction transaction) =>
        new(
            accounts,
            balances,
            ledger,
            transaction,
            new FakeUserRepository(),
            new FakeEmailService(),
            new FakeClock(),
            NullLogger<MoneyTransferService>.Instance);

    private sealed class FakeSavingsAccountRepository : ISavingsAccountRepository
    {
        private readonly Dictionary<Guid, SavingsAccount> _accounts = new();

        public SavingsAccount Seed(Guid id, SavingsAccountType type, string accountNumber)
        {
            var account = new SavingsAccount(id)
            {
                OwnerUserId = "user-1",
                AccountNumber = accountNumber,
                Balance = 1000m,
                Type = type,
                Status = SavingsAccountStatus.Active
            };
            _accounts[id] = account;
            return account;
        }

        public Task<SavingsAccount?> GetByAccountNumberAsync(
            string accountNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(a => a.AccountNumber == accountNumber));

        public Task<SavingsAccount?> GetPrincipalAccountAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(a =>
                a.OwnerUserId == ownerUserId && a.Type == SavingsAccountType.Principal));

        public Task<bool> AccountNumberExistsAsync(
            string accountNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.Any(a => a.AccountNumber == accountNumber));

        public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<SavingsAccount>>(_accounts.Values
                .Where(a => a.OwnerUserId == ownerUserId && a.Status == SavingsAccountStatus.Active)
                .ToArray());

        public Task<PagedResult<SavingsAccount>> GetPagedAsync(
            PagedRequest request,
            string? ownerIdentification = null,
            SavingsAccountStatus? status = null,
            SavingsAccountType? type = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<SavingsAccount>([], request.Page, request.PageSize, 0));

        public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) =>
            _accounts.Values.AsQueryable();

        public Task<SavingsAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.GetValueOrDefault(id));

        public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavingsAccount>>(_accounts.Values.ToArray());

        public Task<SavingsAccount> AddAsync(
            SavingsAccount entity,
            CancellationToken cancellationToken = default)
        {
            _accounts[entity.Id] = entity;
            return Task.FromResult(entity);
        }

        public Task<SavingsAccount?> UpdateAsync(
            Guid id,
            SavingsAccount value,
            CancellationToken cancellationToken = default)
        {
            _accounts[id] = value;
            return Task.FromResult<SavingsAccount?>(value);
        }

        public Task<SavingsAccount?> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _accounts.Remove(id, out var account);
            return Task.FromResult(account);
        }
    }

    private sealed class FakeAccountBalanceService : IAccountBalanceService
    {
        private readonly Dictionary<Guid, OperationResult> _creditResults = new();

        public List<(Guid AccountId, decimal Amount)> Credits { get; } = [];

        public OperationResult DefaultCreditResult { get; set; } = OperationResult.Success();

        public void SetCreditResultForAccount(Guid accountId, OperationResult result) =>
            _creditResults[accountId] = result;

        public Task<OperationResult> CreditAsync(
            Guid accountId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            Credits.Add((accountId, amount));
            var result = _creditResults.TryGetValue(accountId, out var configured) ? configured : DefaultCreditResult;
            return Task.FromResult(result);
        }

        public Task<OperationResult> DebitAsync(
            Guid accountId,
            decimal amount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());
    }

    private sealed class FakeAccountLedger : IAccountLedger
    {
        public List<object> RecordedApprovals { get; } = [];

        public List<object> RecordedRejections { get; } = [];

        public Task RecordApprovedAsync(
            Guid operationId,
            Guid accountId,
            decimal amount,
            TransactionDirection direction,
            FinancialOperationType operationType,
            string? origin,
            string? beneficiary,
            string? actorUserId,
            string? actorRole,
            CancellationToken cancellationToken = default)
        {
            RecordedApprovals.Add(new object());
            return Task.CompletedTask;
        }

        public Task RecordRejectedAsync(
            Guid accountId,
            Guid operationId,
            decimal amount,
            TransactionDirection direction,
            FinancialOperationType operationType,
            string rejectionReason,
            string? actorUserId,
            string? actorRole,
            CancellationToken cancellationToken = default)
        {
            RecordedRejections.Add(new object());
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFinancialTransaction : IFinancialTransaction
    {
        public bool IsCommitted { get; private set; }

        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            var result = await operation(cancellationToken);
            IsCommitted = true;
            return result;
        }

        public async Task<TResult> ExecuteAsync<TResult>(
            IsolationLevel isolationLevel,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            var result = await operation(cancellationToken);
            IsCommitted = true;
            return result;
        }
    }

    private sealed class FakeUserRepository : IGenericRepository<User, string>
    {
        public IQueryable<User> GetAllQueryable(bool trackChanges = false) =>
            Array.Empty<User>().AsQueryable();

        public Task<User?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<IReadOnlyList<User>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>([]);

        public Task<User> AddAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task<User?> UpdateAsync(
            string id,
            User value,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(value);

        public Task<User?> DeleteAsync(
            string id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
    }

    private sealed class FakeEmailService : IEmailService
    {
        public Task SendAsync(EmailRequestDto emailRequestDto) =>
            Task.CompletedTask;
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
