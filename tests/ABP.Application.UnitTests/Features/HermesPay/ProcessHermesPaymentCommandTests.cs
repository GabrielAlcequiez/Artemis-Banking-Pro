using System.Data;
using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Exceptions;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.HermesPay;
using ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Application.Features.HermesPay.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.HermesPay;

public sealed class ProcessHermesPaymentCommandTests
{
    [Fact]
    public void Request_validator_rejects_invalid_financial_and_card_data()
    {
        var validator = new ProcessHermesPaymentRequestValidator();

        var result = validator.Validate(
            new ProcessHermesPaymentRequest(
                Guid.Empty,
                "1234",
                13,
                99,
                "12",
                0m,
                Guid.Empty));

        Assert.False(result.IsValid);
        Assert.DoesNotContain(result.Errors, error => error.PropertyName == "RequestedCommerceId");
        Assert.Contains(result.Errors, error => error.PropertyName == "CardNumber");
        Assert.Contains(result.Errors, error => error.PropertyName == "ExpirationMonth");
        Assert.Contains(result.Errors, error => error.PropertyName == "ExpirationYear");
        Assert.Contains(result.Errors, error => error.PropertyName == "Cvc");
        Assert.Contains(result.Errors, error => error.PropertyName == "TransactionAmount");
        Assert.Contains(result.Errors, error => error.PropertyName == "OperationId");
    }

    [Fact]
    public async Task Approved_payment_updates_card_consumption_account_and_ledger()
    {
        var commerceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var card = CreateCard(debt: 100m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card);
        var accountRepository = new SavingsAccountRepositoryStub(
            new SavingsAccount(accountId)
            {
                OwnerUserId = "commerce-user",
                AccountNumber = "123456789",
                Balance = 500m,
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active
            });
        var balances = new AccountBalanceSpy();
        var ledger = new FakeAccountLedger();
        var unitOfWork = new UnitOfWorkSpy();
        var transaction = new FinancialTransactionSpy();
        var emails = new EmailServiceSpy(() => transaction.Completed);
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            accountRepository,
            balances,
            ledger,
            unitOfWork,
            transaction,
            emails);
        var request = CreateRequest(commerceId, operationId, 250m);

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(request),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(350m, card.Debt);
        Assert.Equal(operationId, result.Value.OperationId);
        Assert.Equal(250m, result.Value.EffectiveAmount);
        var consumption = Assert.Single(cardRepository.Consumptions);
        Assert.Equal(commerceId, consumption.CommerceId);
        Assert.Equal(accountId, consumption.TargetAccountId);
        Assert.Equal(ConsumptionStatus.Approved, consumption.Status);
        Assert.Equal("Tienda Hermes", consumption.CommerceName);
        Assert.Equal("admin-1", consumption.ActorUserId);
        Assert.Equal((accountId, 250m), Assert.Single(balances.Credits));
        var ledgerEntry = Assert.Single(ledger.RecordedApprovals);
        Assert.Equal(FinancialOperationType.HermesPayment, ledgerEntry.OperationType);
        Assert.Equal(TransactionDirection.Credit, ledgerEntry.Direction);
        Assert.Equal("7598", ledgerEntry.Origin);
        Assert.Equal("123456789", ledgerEntry.Beneficiary);
        Assert.Equal(Roles.Administrator.ToString(), ledgerEntry.ActorRole);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
        Assert.Equal(2, emails.Messages.Count);
        Assert.Contains(emails.Messages, email => email.ToEmail == "client@example.test");
        Assert.Contains(emails.Messages, email => email.ToEmail == "hermes@example.test");
        Assert.All(
            emails.Messages,
            email =>
            {
                Assert.Contains("7598", email.Subject + email.Body);
                Assert.DoesNotContain("1589963258467598", email.Subject + email.Body);
                Assert.DoesNotContain("123", email.Subject + email.Body);
            });
    }

    [Fact]
    public async Task Insufficient_credit_records_rejection_without_mutating_balances()
    {
        var commerceId = Guid.NewGuid();
        var card = CreateCard(debt: 950m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card);
        var balances = new AccountBalanceSpy();
        var ledger = new FakeAccountLedger();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            new SavingsAccountRepositoryStub(
                new SavingsAccount(Guid.NewGuid())
                {
                    OwnerUserId = "commerce-user",
                    AccountNumber = "123456789",
                    Status = SavingsAccountStatus.Active,
                    Type = SavingsAccountType.Principal
                }),
            balances,
            ledger,
            unitOfWork,
            new FinancialTransactionSpy());

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, Guid.NewGuid(), 100m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.InsufficientCredit, result.Error);
        Assert.Equal(950m, card.Debt);
        var consumption = Assert.Single(cardRepository.Consumptions);
        Assert.Equal(ConsumptionStatus.Rejected, consumption.Status);
        Assert.Equal(HermesPayErrors.InsufficientCredit.Code, consumption.FailureCode);
        Assert.Equal(100m, consumption.RequestedAmount);
        Assert.Empty(balances.Credits);
        Assert.Empty(ledger.RecordedApprovals);
        Assert.Empty(ledger.RecordedRejections);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Missing_principal_account_records_rejection_when_card_exists()
    {
        var commerceId = Guid.NewGuid();
        var card = CreateCard(debt: 100m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card);
        var ledger = new FakeAccountLedger();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            new SavingsAccountRepositoryStub(null),
            new AccountBalanceSpy(),
            ledger,
            unitOfWork,
            new FinancialTransactionSpy());

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, Guid.NewGuid(), 100m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.PrimaryAccountRequired, result.Error);
        var consumption = Assert.Single(cardRepository.Consumptions);
        Assert.Equal(ConsumptionStatus.Rejected, consumption.Status);
        Assert.Null(consumption.TargetAccountId);
        Assert.Equal(HermesPayErrors.PrimaryAccountRequired.Code, consumption.FailureCode);
        Assert.Empty(ledger.RecordedRejections);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Unknown_card_is_rejected_without_persisting_an_attempt()
    {
        var commerceId = Guid.NewGuid();
        var cardRepository = new CreditCardRepositoryStub(null);
        var ledger = new FakeAccountLedger();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            CreateAccountRepository(Guid.NewGuid()),
            new AccountBalanceSpy(),
            ledger,
            unitOfWork,
            new FinancialTransactionSpy());

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, Guid.NewGuid(), 100m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.CardNotFound, result.Error);
        Assert.Empty(cardRepository.Consumptions);
        Assert.Empty(ledger.RecordedRejections);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Approved_replay_returns_original_receipt_without_applying_payment_again()
    {
        var commerceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var card = CreateCard(debt: 350m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card)
        {
            PreviousConsumption = CreatePersistedConsumption(
                card,
                commerceId,
                accountId,
                operationId,
                250m,
                ConsumptionStatus.Approved)
        };
        var balances = new AccountBalanceSpy();
        var ledger = new FakeAccountLedger();
        var unitOfWork = new UnitOfWorkSpy();
        var emails = new EmailServiceSpy();
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            CreateAccountRepository(accountId),
            balances,
            ledger,
            unitOfWork,
            new FinancialTransactionSpy(),
            emails);

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, operationId, 250m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(operationId, result.Value.OperationId);
        Assert.Equal(350m, card.Debt);
        Assert.Empty(cardRepository.Consumptions);
        Assert.Empty(balances.Credits);
        Assert.Empty(ledger.RecordedApprovals);
        Assert.Empty(ledger.RecordedRejections);
        Assert.Empty(emails.Messages);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Email_failure_after_commit_does_not_change_approved_result()
    {
        var commerceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var card = CreateCard(debt: 100m, limit: 1_000m);
        var transaction = new FinancialTransactionSpy();
        var handler = CreateHandler(
            commerceId,
            new CreditCardRepositoryStub(card),
            CreateAccountRepository(accountId),
            new AccountBalanceSpy(),
            new FakeAccountLedger(),
            new UnitOfWorkSpy(),
            transaction,
            new ThrowingEmailService(() => transaction.Completed));

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, Guid.NewGuid(), 100m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(transaction.Completed);
        Assert.Equal(200m, card.Debt);
    }

    [Fact]
    public async Task Rejected_replay_returns_the_persisted_error_without_new_writes()
    {
        var commerceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var card = CreateCard(debt: 950m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card)
        {
            PreviousConsumption = CreatePersistedConsumption(
                card,
                commerceId,
                accountId,
                operationId,
                100m,
                ConsumptionStatus.Rejected,
                HermesPayErrors.InsufficientCredit)
        };
        var balances = new AccountBalanceSpy();
        var ledger = new FakeAccountLedger();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            CreateAccountRepository(accountId),
            balances,
            ledger,
            unitOfWork,
            new FinancialTransactionSpy());

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, operationId, 100m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.InsufficientCredit, result.Error);
        Assert.Empty(cardRepository.Consumptions);
        Assert.Empty(balances.Credits);
        Assert.Empty(ledger.RecordedApprovals);
        Assert.Empty(ledger.RecordedRejections);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Reusing_operation_id_with_different_amount_returns_conflict()
    {
        var commerceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var card = CreateCard(debt: 100m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card)
        {
            PreviousConsumption = CreatePersistedConsumption(
                card,
                commerceId,
                accountId,
                operationId,
                100m,
                ConsumptionStatus.Approved)
        };
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            CreateAccountRepository(accountId),
            new AccountBalanceSpy(),
            new FakeAccountLedger(),
            new UnitOfWorkSpy(),
            new FinancialTransactionSpy());

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, operationId, 200m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.OperationIdConflict, result.Error);
    }

    [Fact]
    public async Task Concurrent_unique_conflict_recovers_the_committed_replay()
    {
        var commerceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var card = CreateCard(debt: 350m, limit: 1_000m);
        var cardRepository = new CreditCardRepositoryStub(card)
        {
            PreviousConsumption = CreatePersistedConsumption(
                card,
                commerceId,
                accountId,
                operationId,
                250m,
                ConsumptionStatus.Approved)
        };
        var handler = CreateHandler(
            commerceId,
            cardRepository,
            CreateAccountRepository(accountId),
            new AccountBalanceSpy(),
            new FakeAccountLedger(),
            new UnitOfWorkSpy(),
            new ConflictingFinancialTransaction());

        var result = await handler.Handle(
            new ProcessHermesPaymentCommand(
                CreateRequest(commerceId, operationId, 250m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(operationId, result.Value.OperationId);
        Assert.Empty(cardRepository.Consumptions);
    }

    private static ProcessHermesPaymentCommandHandler CreateHandler(
        Guid commerceId,
        ICreditCardRepository cards,
        ISavingsAccountRepository accounts,
        IAccountBalanceService balances,
        IAccountLedger ledger,
        IUnitOfWork unitOfWork,
        IFinancialTransaction transaction,
        IEmailService? emails = null,
        IUserRepository? users = null) =>
        new(
            new FakeCommerceAuthorizationResolverService
            {
                DefaultResult = OperationResult<Guid>.Success(commerceId)
            },
            new CommerceAuthorizationResolverServiceTests.CommerceRepositoryStub
            {
                Detail = CommerceAuthorizationResolverServiceTests.CreateCommerce(commerceId)
            },
            cards,
            accounts,
            balances,
            ledger,
            unitOfWork,
            transaction,
            new CvcServiceStub(),
            new ClockStub(),
            new CommerceAuthorizationResolverServiceTests.CurrentUserStub(
                Roles.Administrator,
                "admin-1"),
            users ?? new CommerceAuthorizationResolverServiceTests.UserRepositoryStub
            {
                User = new User("client-1")
                {
                    Name = "Cliente",
                    LastName = "Hermes",
                    Email = "client@example.test",
                    Role = Roles.Client,
                    IsActive = true
                }
            },
            emails ?? new EmailServiceSpy(),
            NullLogger<ProcessHermesPaymentCommandHandler>.Instance);

    private static ProcessHermesPaymentRequest CreateRequest(
        Guid commerceId,
        Guid operationId,
        decimal amount) =>
        new(
            commerceId,
            "1589963258467598",
            8,
            2029,
            "123",
            amount,
            operationId);

    private static SavingsAccountRepositoryStub CreateAccountRepository(Guid accountId) =>
        new(
            new SavingsAccount(accountId)
            {
                OwnerUserId = "commerce-user",
                AccountNumber = "123456789",
                Status = SavingsAccountStatus.Active,
                Type = SavingsAccountType.Principal
            });

    private static CardConsumption CreatePersistedConsumption(
        CreditCard card,
        Guid commerceId,
        Guid accountId,
        Guid operationId,
        decimal amount,
        ConsumptionStatus status,
        Error? error = null) =>
        new()
        {
            CreditCardId = card.Id,
            CommerceId = commerceId,
            TargetAccountId = accountId,
            CommerceName = "Tienda Hermes",
            RequestedAmount = amount,
            Amount = amount,
            Status = status,
            OccurredAtUtc = new DateTimeOffset(2026, 8, 13, 14, 30, 0, TimeSpan.Zero),
            OperationId = operationId,
            ActorUserId = "admin-1",
            FailureCode = error?.Code,
            FailureDescription = error?.Description
        };

    private static CreditCard CreateCard(decimal debt, decimal limit) =>
        new()
        {
            CardNumber = "1589963258467598",
            CvcHash = "valid-hash",
            ClientId = "client-1",
            AssignedByUserId = "admin-1",
            Debt = debt,
            Limit = limit,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = CreditCardStatus.Active
        };

    private sealed class CreditCardRepositoryStub(CreditCard? card)
        : ICreditCardRepository
    {
        public List<CardConsumption> Consumptions { get; } = [];
        public CardConsumption? PreviousConsumption { get; init; }

        public Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<CreditCard?>(
                card is not null && card.CardNumber == cardNumber ? card : null);
        public Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default)
        {
            Consumptions.Add(consumption);
            return Task.CompletedTask;
        }

        public Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCard?> GetByCreationOperationIdAsync(
            Guid operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                card?.CreationOperationId == operationId ? card : null);
        public Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CardPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CardConsumption?> GetConsumptionByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                PreviousConsumption?.OperationId == operationId
                    ? PreviousConsumption
                    : Consumptions.SingleOrDefault(item => item.OperationId == operationId));
        public Task<IReadOnlyCollection<CreditCard>> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string?> FindClientIdByIdentificationAsync(string identification, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasAnyCardsAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(int page, int pageSize, string? identification = null, CreditCardStatusFilter? status = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCardDetailReadModel?> GetDetailsAsync(Guid creditCardId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(Guid creditCardId, string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsActiveClientAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ClientExistsAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCard?> GetForUpdateAsync(Guid creditCardId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<CreditCard> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<CreditCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<CreditCard>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCard> AddAsync(CreditCard entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCard?> UpdateAsync(Guid id, CreditCard value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CreditCard?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class SavingsAccountRepositoryStub(SavingsAccount? account)
        : ISavingsAccountRepository
    {
        public Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SavingsAccount?>(
                account is not null && account.OwnerUserId == ownerUserId
                    ? account
                    : null);
        public Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(string ownerUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<SavingsAccount>> GetPagedAsync(PagedRequest request, string? ownerIdentification = null, SavingsAccountStatus? status = null, SavingsAccountType? type = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount> AddAsync(SavingsAccount entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount?> UpdateAsync(Guid id, SavingsAccount value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class AccountBalanceSpy : IAccountBalanceService
    {
        public List<(Guid AccountId, decimal Amount)> Credits { get; } = [];
        public Task<OperationResult> CreditAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            Credits.Add((accountId, amount));
            return Task.FromResult(OperationResult.Success());
        }
        public Task<OperationResult> DebitAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class UnitOfWorkSpy : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FinancialTransactionSpy : IFinancialTransaction
    {
        public IsolationLevel? IsolationLevel { get; private set; }
        public bool Completed { get; private set; }
        public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            var result = await operation(cancellationToken);
            Completed = true;
            return result;
        }
        public async Task<TResult> ExecuteAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            IsolationLevel = isolationLevel;
            var result = await operation(cancellationToken);
            Completed = true;
            return result;
        }
    }

    private sealed class ConflictingFinancialTransaction : IFinancialTransaction
    {
        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) =>
            throw new PersistenceConflictException();

        public Task<TResult> ExecuteAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) =>
            throw new PersistenceConflictException();
    }

    private sealed class EmailServiceSpy(Func<bool>? transactionCompleted = null)
        : IEmailService
    {
        public List<EmailRequestDto> Messages { get; } = [];

        public Task SendAsync(EmailRequestDto emailRequestDto)
        {
            if (transactionCompleted is not null && !transactionCompleted())
            {
                throw new InvalidOperationException("El correo se intentó enviar antes del commit.");
            }

            Messages.Add(emailRequestDto);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailService(Func<bool> transactionCompleted)
        : IEmailService
    {
        public Task SendAsync(EmailRequestDto emailRequestDto)
        {
            Assert.True(transactionCompleted());
            throw new InvalidOperationException("Fallo SMTP simulado.");
        }
    }

    private sealed class CvcServiceStub : ICvcService
    {
        public string Generate() => "123";
        public string Hash(string cvc) => "valid-hash";
        public bool Verify(string cvc, string cvcHash) => cvc == "123" && cvcHash == "valid-hash";
    }

    private sealed class ClockStub : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 13, 14, 30, 0, TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => new(2026, 8, 13);
    }
}
