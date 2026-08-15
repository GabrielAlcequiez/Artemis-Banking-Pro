using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.Infrastructure.Persistence.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CardFinancialTransactionSqlTests : IAsyncLifetime
{
    private readonly string connectionString;
    private Guid accountId;
    private Guid cardId;

    public CardFinancialTransactionSqlTests()
    {
        var configuredConnection = Environment.GetEnvironmentVariable(
            "ABP_TEST_SQL_CONNECTION");
        var builder = string.IsNullOrWhiteSpace(configuredConnection)
            ? new SqlConnectionStringBuilder
            {
                DataSource = "localhost",
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            }
            : new SqlConnectionStringBuilder(configuredConnection);

        builder.InitialCatalog =
            $"ABP_CardFinanceTests_{Guid.NewGuid():N}";
        connectionString = builder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        context.Users.AddRange(
            CreateUser("client-1", Roles.Client, "00100000001"),
            CreateUser("admin-1", Roles.Administrator, "00100000002"));
        var account = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "client-1",
            AccountNumber = "123456789",
            Balance = 1_000m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        var card = new CreditCard
        {
            ClientId = "client-1",
            CardNumber = "4000000000001234",
            CvcHash = "safe-test-hash",
            Limit = 2_000m,
            Debt = 500m,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = CreditCardStatus.Active,
            AssignedByUserId = "admin-1"
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
    public async Task Ledger_failure_rolls_back_account_card_and_payment()
    {
        await using (var operationContext = CreateContext())
        {
            var unitOfWork = new UnitOfWork(operationContext);
            var accounts = new SavingsAccountRepository(operationContext);
            var service = new CardPaymentService(
                new CreditCardRepository(operationContext),
                accounts,
                new UserRepository(operationContext),
                new AccountBalanceService(
                    accounts,
                    unitOfWork,
                    NullLogger<AccountBalanceService>.Instance),
                new ThrowingLedger(),
                unitOfWork,
                new EfFinancialTransaction(operationContext),
                new TestCurrentUser(),
                new TestClock(),
                new CreditCardPaymentRequestValidator(),
                new NoOpEmailService(),
                NullLogger<CardPaymentService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ProcessPaymentAsync(
                    new CreditCardPaymentRequest(
                        cardId,
                        accountId,
                        100m,
                        Guid.NewGuid())));

            Assert.Empty(operationContext.ChangeTracker.Entries());
        }

        await using var verificationContext = CreateContext();
        var account = await verificationContext.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == accountId);
        var card = await verificationContext.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == cardId);
        Assert.Equal(1_000m, account.Balance);
        Assert.Equal(500m, card.Debt);
        Assert.Empty(verificationContext.CardPayments);
        Assert.Empty(verificationContext.AccountTransactions);
    }

    private AppDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options);

    private static User CreateUser(
        string id,
        Roles role,
        string identification) =>
        new(id)
        {
            Name = "Usuario",
            LastName = "Prueba",
            Email = $"{id}@example.com",
            UserName = id,
            Identification = identification,
            Role = role,
            IsActive = true
        };

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public string? UserId => "client-1";
        public string? UserName => "client-1";
        public Guid? CommerceId => null;
        public IReadOnlyCollection<string> Roles => [nameof(ABP.Domain.Enums.Roles.Client)];
        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow =>
            new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => new(2026, 8, 11);
    }

    private sealed class ThrowingLedger : IAccountLedger
    {
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
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Fallo de ledger simulado.");

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
}
