using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Exceptions;
using ABP.Application.Features.Accounts.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Commerce.Services.Interfaces;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.HermesPay;
using ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.Infrastructure.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class HermesFinancialTransactionSqlTests : IAsyncLifetime
{
    private const string TestCvcHash =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private readonly string connectionString;
    private Guid accountId;
    private Guid cardId;
    private Guid commerceId;

    public HermesFinancialTransactionSqlTests()
    {
        connectionString = TestDatabase.CreateConnectionString(
            $"ABP_HermesFinanceTests_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var commerce = new CommerceEntity
        {
            Name = "Tienda Hermes SQL",
            Email = "hermes-sql@example.test",
            PhoneNumber = "8095551234",
            Rnc = "123456789",
            Status = CommerceStatus.Active
        };
        context.Commerces.Add(commerce);
        await context.SaveChangesAsync();
        commerceId = commerce.Id;

        context.Users.AddRange(
            CreateUser("commerce-hermes", Roles.Commerce, "00100000001", commerceId),
            CreateUser("admin-hermes", Roles.Administrator, "00100000002"),
            CreateUser("client-hermes", Roles.Client, "00100000003"));
        var account = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "commerce-hermes",
            AccountNumber = "123456789",
            Balance = 500m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        var card = new CreditCard
        {
            ClientId = "client-hermes",
            CardNumber = "4000000000009876",
            CvcHash = TestCvcHash,
            Limit = 2_000m,
            Debt = 100m,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = CreditCardStatus.Active,
            AssignedByUserId = "admin-hermes"
        };
        context.SavingsAccounts.Add(account);
        context.CreditCards.Add(card);
        await context.SaveChangesAsync();
        accountId = account.Id;
        cardId = card.Id;
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Ledger_failure_rolls_back_account_card_and_consumption()
    {
        await using (var operationContext = CreateContext())
        {
            var unitOfWork = new UnitOfWork(operationContext);
            var accounts = new SavingsAccountRepository(operationContext);
            var ledger = new ThrowingLedger();
            var handler = new ProcessHermesPaymentCommandHandler(
                new AuthorizationResolverStub(commerceId),
                new CommerceRepository(operationContext),
                new CreditCardRepository(operationContext),
                accounts,
                new AccountBalanceService(
                    accounts,
                    unitOfWork,
                    NullLogger<AccountBalanceService>.Instance),
                ledger,
                unitOfWork,
                new EfFinancialTransaction(operationContext),
                new CvcServiceStub(),
                new TestClock(),
                new TestCurrentUser(),
                new UserRepository(operationContext),
                new NoOpEmailService(),
                NullLogger<ProcessHermesPaymentCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(
                    new ProcessHermesPaymentCommand(
                        new ProcessHermesPaymentRequest(
                            commerceId,
                            "4000000000009876",
                            8,
                            2029,
                            "123",
                            250m,
                            Guid.NewGuid())),
                    CancellationToken.None));

            Assert.True(ledger.RecordApprovedWasCalled);
            Assert.Empty(operationContext.ChangeTracker.Entries());
        }

        await using var verificationContext = CreateContext();
        var account = await verificationContext.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == accountId);
        var card = await verificationContext.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == cardId);

        Assert.Equal(500m, account.Balance);
        Assert.Equal(100m, card.Debt);
        Assert.Empty(verificationContext.CardConsumptions);
        Assert.Empty(verificationContext.AccountTransactions);
    }

    [Fact]
    public async Task Concurrent_payments_never_exceed_card_limit()
    {
        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var coordinator = new CreditCoordinator();
        var firstHandler = CreateHandler(
            firstContext,
            new CoordinatedBalanceService(
                CreateBalanceService(firstContext),
                coordinator));
        var secondHandler = CreateHandler(
            secondContext,
            new CoordinatedBalanceService(
                CreateBalanceService(secondContext),
                coordinator));

        var outcomes = await Task.WhenAll(
            CaptureAsync(firstHandler, Guid.NewGuid(), 1_000m),
            CaptureAsync(secondHandler, Guid.NewGuid(), 1_000m));

        Assert.Single(outcomes, outcome => outcome.Result?.IsSuccess == true);
        var unsuccessfulOutcome = Assert.Single(
            outcomes,
            outcome => outcome.Result?.IsSuccess != true);

        if (unsuccessfulOutcome.Result is { } failedResult)
        {
            Assert.True(failedResult.IsFailure);
            Assert.Equal(HermesPayErrors.InsufficientCredit, failedResult.Error);
            Assert.Null(unsuccessfulOutcome.Exception);
        }
        else
        {
            Assert.True(
                unsuccessfulOutcome.Exception is FinancialConcurrencyException,
                unsuccessfulOutcome.Exception?.ToString());
        }

        await using var verificationContext = CreateContext();
        var account = await verificationContext.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == accountId);
        var card = await verificationContext.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == cardId);
        var approvedConsumptions = await verificationContext.CardConsumptions
            .AsNoTracking()
            .CountAsync(item => item.Status == ConsumptionStatus.Approved);
        var approvedLedgerEntries = await verificationContext.AccountTransactions
            .AsNoTracking()
            .CountAsync(item =>
                item.OperationType == FinancialOperationType.HermesPayment &&
                item.Status == TransactionStatus.Approved);

        Assert.Equal(1_500m, account.Balance);
        Assert.Equal(1_100m, card.Debt);
        Assert.True(card.Debt <= card.Limit);
        Assert.Equal(1, approvedConsumptions);
        Assert.Equal(1, approvedLedgerEntries);
    }

    private ProcessHermesPaymentCommandHandler CreateHandler(
        AppDbContext context,
        IAccountBalanceService balanceService)
    {
        var unitOfWork = new UnitOfWork(context);

        return new ProcessHermesPaymentCommandHandler(
            new AuthorizationResolverStub(commerceId),
            new CommerceRepository(context),
            new CreditCardRepository(context),
            new SavingsAccountRepository(context),
            balanceService,
            new AccountLedger(
                new AccountTransactionRepository(context),
                unitOfWork,
                NullLogger<AccountLedger>.Instance),
            unitOfWork,
            new EfFinancialTransaction(context),
            new CvcServiceStub(),
            new TestClock(),
            new TestCurrentUser(),
            new UserRepository(context),
            new NoOpEmailService(),
            NullLogger<ProcessHermesPaymentCommandHandler>.Instance);
    }

    private static IAccountBalanceService CreateBalanceService(AppDbContext context)
    {
        var unitOfWork = new UnitOfWork(context);
        return new AccountBalanceService(
            new SavingsAccountRepository(context),
            unitOfWork,
            NullLogger<AccountBalanceService>.Instance);
    }

    private async Task<ConcurrentOutcome> CaptureAsync(
        ProcessHermesPaymentCommandHandler handler,
        Guid operationId,
        decimal amount)
    {
        try
        {
            var result = await handler.Handle(
                new ProcessHermesPaymentCommand(
                    new ProcessHermesPaymentRequest(
                        commerceId,
                        "4000000000009876",
                        8,
                        2029,
                        "123",
                        amount,
                        operationId)),
                CancellationToken.None);
            return new ConcurrentOutcome(result, null);
        }
        catch (Exception exception)
        {
            return new ConcurrentOutcome(null, exception);
        }
    }

    private AppDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options);

    private static User CreateUser(
        string id,
        Roles role,
        string identification,
        Guid? associatedCommerceId = null) =>
        new(id)
        {
            Name = "Usuario",
            LastName = "Hermes",
            Email = $"{id}@example.test",
            UserName = id,
            Identification = identification,
            Role = role,
            IsActive = true,
            CommerceId = associatedCommerceId
        };

    private sealed class AuthorizationResolverStub(Guid authorizedCommerceId)
        : ICommerceAuthorizationResolverService
    {
        public Task<OperationResult<Guid>> ResolveAuthorizedCommerceIdAsync(
            Guid requestedCommerceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<Guid>.Success(authorizedCommerceId));
    }

    private sealed class CvcServiceStub : ICvcService
    {
        public string Generate() => "123";
        public string Hash(string cvc) => TestCvcHash;
        public bool Verify(string cvc, string cvcHash) =>
            cvc == "123" && cvcHash == TestCvcHash;
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public string? UserId => "admin-hermes";
        public string? UserName => "admin-hermes";
        public Guid? CommerceId => null;
        public IReadOnlyCollection<string> Roles => [nameof(ABP.Domain.Enums.Roles.Administrator)];
        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow =>
            new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => new(2026, 8, 13);
    }

    private sealed class ThrowingLedger : IAccountLedger
    {
        public bool RecordApprovedWasCalled { get; private set; }

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
            RecordApprovedWasCalled = true;
            throw new InvalidOperationException("Fallo de ledger simulado.");
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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendAsync(EmailRequestDto emailRequestDto) =>
            Task.CompletedTask;
    }

    private sealed class CreditCoordinator
    {
        private readonly TaskCompletionSource ready = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        public Task ArriveAsync()
        {
            if (Interlocked.Increment(ref arrivals) == 2)
            {
                ready.TrySetResult();
            }

            return ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    private sealed class CoordinatedBalanceService(
        IAccountBalanceService inner,
        CreditCoordinator coordinator) : IAccountBalanceService
    {
        public async Task<OperationResult> CreditAsync(
            Guid accountId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            await coordinator.ArriveAsync();
            return await inner.CreditAsync(accountId, amount, cancellationToken);
        }

        public Task<OperationResult> DebitAsync(
            Guid accountId,
            decimal amount,
            CancellationToken cancellationToken = default) =>
            inner.DebitAsync(accountId, amount, cancellationToken);
    }

    private sealed record ConcurrentOutcome(
        OperationResult<FinancialOperationReceipt>? Result,
        Exception? Exception);
}
