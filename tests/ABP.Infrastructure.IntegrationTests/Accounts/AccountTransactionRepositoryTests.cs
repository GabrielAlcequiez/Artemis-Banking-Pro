using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Accounts;

public sealed class AccountTransactionRepositoryTests : IAsyncLifetime
{
    #region Test setup

    private readonly string _databaseName = $"ABP_AccountTransactionRepoTests_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;
    private AccountTransactionRepository _repository = null!;

    private static readonly DateOnly Today = new(2026, 6, 15);
    private static readonly DateOnly Yesterday = Today.AddDays(-1);

    public AccountTransactionRepositoryTests()
    {
        _connectionString = TestDatabase.CreateConnectionString(_databaseName);
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new AccountTransactionRepository(_context);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    #endregion

    #region AddAsync + GetPagedByAccountAsync

    [Fact]
    public async Task GetPagedByAccountAsync_returns_transactions_in_descending_created_order()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetPagedByAccountAsync(seeded.AccountA.Id, new PagedRequest(1, 20));

        Assert.Equal(6, result.TotalRecords);
        Assert.Equal(
            [
                seeded.TodaySecondActor1Deposit.Id,
                seeded.TodayOtherActorDeposit.Id,
                seeded.TodayDebitLeg.Id,
                seeded.YesterdayDeposit.Id,
                seeded.OldWithdrawal.Id,
                seeded.OldDeposit.Id
            ],
            result.Data.Select(t => t.Id).ToArray());
    }

    #endregion

    #region GetByOperationIdAsync

    [Fact]
    public async Task GetByOperationIdAsync_returns_both_legs_of_a_transfer()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetByOperationIdAsync(seeded.TransferOperationId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == seeded.TodayDebitLeg.Id && t.Direction == TransactionDirection.Debit);
        Assert.Contains(result, t => t.Id == seeded.TodayCreditLegOnOtherAccount.Id && t.Direction == TransactionDirection.Credit);
    }

    #endregion

    #region GetMostRecentByAccountAsync

    [Fact]
    public async Task GetMostRecentByAccountAsync_respects_the_count_limit()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetMostRecentByAccountAsync(seeded.AccountA.Id, count: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(
            [seeded.TodaySecondActor1Deposit.Id, seeded.TodayOtherActorDeposit.Id],
            result.Select(t => t.Id).ToArray());
    }

    #endregion

    #region CountByActorTodayAsync / SumAmountByActorTodayAsync

    [Fact]
    public async Task CountByActorTodayAsync_excludes_yesterday_and_other_actors()
    {
        var seeded = await SeedAsync(_context);

        var count = await _repository.CountByActorTodayAsync("actor-1", Today);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task SumAmountByActorTodayAsync_excludes_yesterday_and_other_actors()
    {
        var seeded = await SeedAsync(_context);

        var sum = await _repository.SumAmountByActorTodayAsync("actor-1", Today);

        Assert.Equal(130m, sum);
    }

    #endregion

    #region Test data builders

    private static async Task<SeededTransactions> SeedAsync(AppDbContext context)
    {
        context.Users.AddRange(
            new User("actor-1")
            {
                Name = "María",
                LastName = "Gómez",
                Identification = "11111111111",
                Email = "maria@example.test",
                UserName = "maria",
                IsActive = true,
                Role = Roles.Client
            },
            new User("actor-2")
            {
                Name = "Pedro",
                LastName = "Díaz",
                Identification = "22222222222",
                Email = "pedro@example.test",
                UserName = "pedro",
                IsActive = true,
                Role = Roles.Client
            });

        var accountA = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "actor-1",
            AccountNumber = "300000001",
            Balance = 1000m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        var accountB = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "actor-2",
            AccountNumber = "300000002",
            Balance = 500m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        context.SavingsAccounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        var oldDeposit = AddTransaction(
            context, accountA.Id, Guid.NewGuid(), 100m, TransactionDirection.Credit,
            FinancialOperationType.Deposit, "actor-1", new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero));

        var oldWithdrawal = AddTransaction(
            context, accountA.Id, Guid.NewGuid(), 200m, TransactionDirection.Debit,
            FinancialOperationType.Withdrawal, "actor-1", new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero));

        var yesterdayDeposit = AddTransaction(
            context, accountA.Id, Guid.NewGuid(), 300m, TransactionDirection.Credit,
            FinancialOperationType.Deposit, "actor-1", new DateTimeOffset(Yesterday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        var transferOperationId = Guid.NewGuid();
        var todayDebitLeg = AddTransaction(
            context, accountA.Id, transferOperationId, 50m, TransactionDirection.Debit,
            FinancialOperationType.ThirdPartyTransfer, "actor-1", new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue).AddHours(9), TimeSpan.Zero));
        var todayCreditLegOnOtherAccount = AddTransaction(
            context, accountB.Id, transferOperationId, 50m, TransactionDirection.Credit,
            FinancialOperationType.ThirdPartyTransfer, "actor-1", new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue).AddHours(9), TimeSpan.Zero));

        var todayOtherActorDeposit = AddTransaction(
            context, accountA.Id, Guid.NewGuid(), 75m, TransactionDirection.Credit,
            FinancialOperationType.Deposit, "actor-2", new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue).AddHours(11), TimeSpan.Zero));

        var todaySecondActor1Deposit = AddTransaction(
            context, accountA.Id, Guid.NewGuid(), 30m, TransactionDirection.Credit,
            FinancialOperationType.Deposit, "actor-1", new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue).AddHours(13), TimeSpan.Zero));

        await context.SaveChangesAsync();

        return new(
            accountA,
            accountB,
            oldDeposit,
            oldWithdrawal,
            yesterdayDeposit,
            todayDebitLeg,
            todayCreditLegOnOtherAccount,
            todayOtherActorDeposit,
            todaySecondActor1Deposit,
            transferOperationId);
    }

    private static AccountTransaction AddTransaction(
        AppDbContext context,
        Guid accountId,
        Guid operationId,
        decimal amount,
        TransactionDirection direction,
        FinancialOperationType operationType,
        string actorUserId,
        DateTimeOffset createdAt)
    {
        var transaction = new AccountTransaction(Guid.NewGuid())
        {
            AccountId = accountId,
            OperationId = operationId,
            Amount = amount,
            Direction = direction,
            OperationType = operationType,
            Status = TransactionStatus.Approved,
            ActorUserId = actorUserId,
            ActorRole = "Client"
        };

        context.AccountTransactions.Add(transaction);
        context.Entry(transaction).Property(t => t.CreatedAtUtc).CurrentValue = createdAt;
        return transaction;
    }

    private sealed record SeededTransactions(
        SavingsAccount AccountA,
        SavingsAccount AccountB,
        AccountTransaction OldDeposit,
        AccountTransaction OldWithdrawal,
        AccountTransaction YesterdayDeposit,
        AccountTransaction TodayDebitLeg,
        AccountTransaction TodayCreditLegOnOtherAccount,
        AccountTransaction TodayOtherActorDeposit,
        AccountTransaction TodaySecondActor1Deposit,
        Guid TransferOperationId);

    #endregion
}
