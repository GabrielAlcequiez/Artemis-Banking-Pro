using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CardFinancialOperationsTests
{
    [Fact]
    public async Task Client_payment_caps_overpayment_and_is_idempotent()
    {
        await using var environment = CreateEnvironment("client-1", Roles.Client);
        var products = await environment.SeedProductsAsync(
            "client-1",
            "client-1",
            accountBalance: 1_000m,
            cardDebt: 500m,
            cardLimit: 2_000m);
        var operationId = Guid.NewGuid();
        var service = environment.CreatePaymentService();
        var options = await service.GetClientOptionsAsync();
        var request = new CreditCardPaymentRequest(
            products.CardId,
            products.AccountId,
            1_000m,
            operationId);

        var firstResult = await service.ProcessPaymentAsync(request);
        var retryResult = await service.ProcessPaymentAsync(request);

        Assert.Single(options.CreditCards);
        Assert.Single(options.SavingsAccounts);
        Assert.Equal("************", options.CreditCards.Single().MaskedCardNumber[..12]);
        Assert.True(firstResult.IsSuccess);
        Assert.True(retryResult.IsSuccess);
        Assert.Equal(500m, firstResult.Value.EffectiveAmount);
        Assert.Equal(500m, retryResult.Value.EffectiveAmount);

        var account = await environment.Context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.AccountId);
        var card = await environment.Context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.CardId);
        Assert.Equal(500m, account.Balance);
        Assert.Equal(0m, card.Debt);
        Assert.Single(environment.Context.CardPayments);
        Assert.Single(environment.Context.AccountTransactions);
    }

    [Fact]
    public async Task Client_payment_rejects_an_account_owned_by_another_client()
    {
        await using var environment = CreateEnvironment("client-1", Roles.Client);
        var products = await environment.SeedProductsAsync(
            cardOwnerId: "client-1",
            accountOwnerId: "client-2",
            accountBalance: 1_000m,
            cardDebt: 500m,
            cardLimit: 2_000m);
        var service = environment.CreatePaymentService();

        var result = await service.ProcessPaymentAsync(
            new CreditCardPaymentRequest(
                products.CardId,
                products.AccountId,
                100m,
                Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("CreditCards.OwnershipRequired", result.Error.Code);
        Assert.Empty(environment.Context.CardPayments);
        Assert.Empty(environment.Context.AccountTransactions);
    }

    [Fact]
    public async Task Payment_with_insufficient_funds_records_rejection_without_mutating_debt()
    {
        await using var environment = CreateEnvironment("client-1", Roles.Client);
        var products = await environment.SeedProductsAsync(
            "client-1",
            "client-1",
            accountBalance: 50m,
            cardDebt: 500m,
            cardLimit: 2_000m);
        var service = environment.CreatePaymentService();

        var result = await service.ProcessPaymentAsync(
            new CreditCardPaymentRequest(
                products.CardId,
                products.AccountId,
                100m,
                Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("CreditCards.InsufficientFunds", result.Error.Code);
        var account = await environment.Context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.AccountId);
        var card = await environment.Context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.CardId);
        var ledgerEntry = await environment.Context.AccountTransactions
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(50m, account.Balance);
        Assert.Equal(500m, card.Debt);
        Assert.Equal(TransactionStatus.Rejected, ledgerEntry.Status);
        Assert.Empty(environment.Context.CardPayments);
    }

    [Fact]
    public async Task Cashier_payment_allows_different_account_and_card_owners()
    {
        await using var environment = CreateEnvironment("cashier-1", Roles.Cashier);
        var products = await environment.SeedProductsAsync(
            cardOwnerId: "client-1",
            accountOwnerId: "client-2",
            accountBalance: 1_000m,
            cardDebt: 300m,
            cardLimit: 2_000m);
        await environment.AddUserAsync("cashier-1", Roles.Cashier);
        var service = environment.CreatePaymentService();
        var accountBeforePayment = await environment.Context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.AccountId);
        var cardBeforePayment = await environment.Context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.CardId);

        var preview = await service.PrepareCashierPaymentAsync(
            accountBeforePayment.AccountNumber,
            cardBeforePayment.CardNumber,
            500m,
            Guid.NewGuid());

        var result = await service.ProcessPaymentAsync(
            new CreditCardPaymentRequest(
                products.CardId,
                products.AccountId,
                100m,
                Guid.NewGuid()));

        Assert.True(preview.IsSuccess);
        Assert.Equal(cardBeforePayment.CardNumber[^4..], preview.Value.CardLastFourDigits);
        Assert.Equal(300m, preview.Value.EffectiveAmount);
        Assert.True(result.IsSuccess);
        var account = await environment.Context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.AccountId);
        var card = await environment.Context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.CardId);
        Assert.Equal(900m, account.Balance);
        Assert.Equal(200m, card.Debt);
    }

    [Fact]
    public async Task Cash_advance_credits_principal_and_charges_principal_plus_interest()
    {
        await using var environment = CreateEnvironment("client-1", Roles.Client);
        var products = await environment.SeedProductsAsync(
            "client-1",
            "client-1",
            accountBalance: 50m,
            cardDebt: 100m,
            cardLimit: 1_000m);
        var service = environment.CreateCashAdvanceService();

        var result = await service.ProcessCashAdvanceAsync(
            new CashAdvanceRequest(
                products.CardId,
                products.AccountId,
                100m,
                Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        var account = await environment.Context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.AccountId);
        var card = await environment.Context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.CardId);
        var consumption = await environment.Context.CardConsumptions
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(150m, account.Balance);
        Assert.Equal(206.25m, card.Debt);
        Assert.Equal(106.25m, consumption.Amount);
        Assert.Equal("AVANCE", consumption.CommerceName);
        Assert.Equal(ConsumptionStatus.Approved, consumption.Status);
    }

    [Fact]
    public async Task Cash_advance_with_insufficient_credit_records_rejection_without_mutating_balances()
    {
        await using var environment = CreateEnvironment("client-1", Roles.Client);
        var products = await environment.SeedProductsAsync(
            "client-1",
            "client-1",
            accountBalance: 50m,
            cardDebt: 900m,
            cardLimit: 1_000m);
        var service = environment.CreateCashAdvanceService();

        var result = await service.ProcessCashAdvanceAsync(
            new CashAdvanceRequest(
                products.CardId,
                products.AccountId,
                100m,
                Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("CreditCards.InsufficientCredit", result.Error.Code);
        var account = await environment.Context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.AccountId);
        var card = await environment.Context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == products.CardId);
        var consumption = await environment.Context.CardConsumptions
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(50m, account.Balance);
        Assert.Equal(900m, card.Debt);
        Assert.Equal(ConsumptionStatus.Rejected, consumption.Status);
        Assert.Empty(environment.Context.AccountTransactions);
    }

    private static TestEnvironment CreateEnvironment(
        string userId,
        Roles role) =>
        new(userId, role);

    private sealed class TestEnvironment : IAsyncDisposable
    {
        private readonly StubCurrentUser currentUser;
        private readonly StubClock clock = new();
        private readonly CreditCardRepository creditCards;
        private readonly SavingsAccountRepository accounts;
        private readonly UserRepository users;
        private readonly AccountTransactionRepository transactions;
        private readonly UnitOfWork unitOfWork;
        private readonly AccountBalanceService balances;
        private readonly AccountLedger ledger;
        private readonly EfFinancialTransaction financialTransaction;

        public TestEnvironment(string userId, Roles role)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"CardOperations_{Guid.NewGuid():N}")
                .Options;
            Context = new AppDbContext(options);
            currentUser = new StubCurrentUser(userId, role);
            creditCards = new CreditCardRepository(Context);
            accounts = new SavingsAccountRepository(Context);
            users = new UserRepository(Context);
            transactions = new AccountTransactionRepository(Context);
            unitOfWork = new UnitOfWork(Context);
            balances = new AccountBalanceService(
                accounts,
                unitOfWork,
                NullLogger<AccountBalanceService>.Instance);
            ledger = new AccountLedger(
                transactions,
                unitOfWork,
                NullLogger<AccountLedger>.Instance);
            financialTransaction = new EfFinancialTransaction(Context);
        }

        public AppDbContext Context { get; }

        public CardPaymentService CreatePaymentService() =>
            new(
                creditCards,
                accounts,
                users,
                transactions,
                balances,
                ledger,
                unitOfWork,
                financialTransaction,
                currentUser,
                clock,
                new CreditCardPaymentRequestValidator());

        public CashAdvanceService CreateCashAdvanceService() =>
            new(
                creditCards,
                accounts,
                balances,
                ledger,
                unitOfWork,
                financialTransaction,
                currentUser,
                clock,
                new CashAdvanceRequestValidator());

        public async Task<(Guid CardId, Guid AccountId)> SeedProductsAsync(
            string cardOwnerId,
            string accountOwnerId,
            decimal accountBalance,
            decimal cardDebt,
            decimal cardLimit)
        {
            await AddUserAsync(cardOwnerId, Roles.Client);
            if (accountOwnerId != cardOwnerId)
            {
                await AddUserAsync(accountOwnerId, Roles.Client);
            }

            var account = new SavingsAccount(Guid.NewGuid())
            {
                OwnerUserId = accountOwnerId,
                AccountNumber = Random.Shared.NextInt64(100_000_000, 999_999_999).ToString(),
                Balance = accountBalance,
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active,
                RowVersion = [1]
            };
            var card = new CreditCard
            {
                ClientId = cardOwnerId,
                AssignedByUserId = "admin-1",
                CardNumber = $"4{Random.Shared.NextInt64(0, 999_999_999_999_999):D15}",
                CvcHash = new string('a', 64),
                Limit = cardLimit,
                Debt = cardDebt,
                ExpirationDate = new DateOnly(2030, 12, 31),
                Status = CreditCardStatus.Active,
                RowVersion = [1]
            };
            Context.SavingsAccounts.Add(account);
            Context.CreditCards.Add(card);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return (card.Id, account.Id);
        }

        public async Task AddUserAsync(string id, Roles role)
        {
            if (await Context.Users.AnyAsync(user => user.Id == id))
            {
                return;
            }

            Context.Users.Add(new User(id)
            {
                Name = "Nombre",
                LastName = id,
                Email = $"{id}@example.com",
                UserName = id,
                Identification = Math.Abs(id.GetHashCode())
                    .ToString()
                    .PadLeft(11, '0')[..11],
                Role = role,
                IsActive = true
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private sealed class StubCurrentUser(string userId, Roles role)
        : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public string? UserId => userId;
        public string? UserName => userId;
        public Guid? CommerceId => null;
        public IReadOnlyCollection<string> Roles => [role.ToString()];
        public bool IsInRole(string requestedRole) =>
            string.Equals(role.ToString(), requestedRole, StringComparison.Ordinal);
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow =>
            new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => new(2026, 8, 11);
    }
}
