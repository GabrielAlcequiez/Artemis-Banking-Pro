using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.CreditCards.Commands.UpdateCreditLimit;

public sealed class UpdateCreditLimitCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_update_limit_request_rules()
    {
        var validator = new UpdateCreditLimitCommandValidator(
            new UpdateCreditLimitRequestValidator());
        var command = new UpdateCreditLimitCommand(
            new UpdateCreditLimitRequest(Guid.Empty, 0m));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.CreditCardId");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.CreditLimit");
    }

    [Fact]
    public async Task Handler_updates_active_card_and_commits_once()
    {
        var cardId = Guid.NewGuid();
        var card = CreateCard(CreditCardStatus.Active, 150m, 500m);
        var repository = new StubCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new StubUnitOfWork();
        var emails = new RecordingCardEmailService
        {
            IsOperationCommitted = () => unitOfWork.SaveCalls == 1
        };
        var handler = CreateHandler(repository, unitOfWork, emails);

        var result = await handler.Handle(
            CreateCommand(cardId, 750m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cardId, repository.ReceivedCreditCardId);
        Assert.Equal(750m, card.Limit);
        Assert.Equal(1, unitOfWork.SaveCalls);
        var email = Assert.Single(emails.SentEmails);
        Assert.False(emails.WasCalledBeforeCommit);
        Assert.Contains("1234", email.Body);
        Assert.Contains("750.00", email.Body);
        Assert.Contains("08/08/2026", email.Body);
        Assert.DoesNotContain(card.CardNumber, email.Subject + email.Body);
        Assert.DoesNotContain(card.CvcHash, email.Subject + email.Body);
    }

    [Fact]
    public async Task Handler_email_failure_does_not_reverse_limit_change()
    {
        var card = CreateCard(CreditCardStatus.Active, 150m, 500m);
        var unitOfWork = new StubUnitOfWork();
        var emails = new RecordingCardEmailService { ThrowOnSend = true };
        var handler = CreateHandler(
            new StubCreditCardRepository { CardForUpdate = card },
            unitOfWork,
            emails);

        var result = await handler.Handle(
            CreateCommand(Guid.NewGuid(), 750m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(750m, card.Limit);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(1, emails.SendAttempts);
    }

    [Fact]
    public async Task Handler_rejects_limit_below_debt_without_mutating_or_committing()
    {
        var cardId = Guid.NewGuid();
        var card = CreateCard(
            CreditCardStatus.Active,
            debt: 500m,
            limit: 700m);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(
            new StubCreditCardRepository { CardForUpdate = card },
            unitOfWork);

        var result = await handler.Handle(
            CreateCommand(cardId, 499m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.LimitBelowDebt, result.Error);
        Assert.Equal(700m, card.Limit);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_rejects_cancelled_card_without_mutating_or_committing()
    {
        var cardId = Guid.NewGuid();
        var card = CreateCard(
            CreditCardStatus.Cancelled,
            debt: 0m,
            limit: 500m);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(
            new StubCreditCardRepository { CardForUpdate = card },
            unitOfWork);

        var result = await handler.Handle(
            CreateCommand(cardId, 750m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.Cancelled, result.Error);
        Assert.Equal(500m, card.Limit);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_returns_not_found_without_committing()
    {
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(
            new StubCreditCardRepository(),
            unitOfWork);

        var result = await handler.Handle(
            CreateCommand(Guid.NewGuid(), 750m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    private static UpdateCreditLimitCommand CreateCommand(
        Guid cardId,
        decimal creditLimit) =>
        new(new UpdateCreditLimitRequest(cardId, creditLimit));

    private static UpdateCreditLimitCommandHandler CreateHandler(
        StubCreditCardRepository repository,
        StubUnitOfWork unitOfWork,
        RecordingCardEmailService? emails = null)
    {
        var users = new StubCardUserRepository();
        users.Users["client-1"] = new User("client-1")
        {
            Name = "Ana",
            LastName = "Pérez",
            Email = "client@example.com",
            Role = Roles.Client,
            IsActive = true
        };

        return new UpdateCreditLimitCommandHandler(
            repository,
            unitOfWork,
            users,
            emails ?? new RecordingCardEmailService(),
            new StubClock(),
            NullLogger<UpdateCreditLimitCommandHandler>.Instance);
    }

    private static CreditCard CreateCard(
        CreditCardStatus status,
        decimal debt,
        decimal limit) =>
        new()
        {
            ClientId = "client-1",
            CardNumber = "4111111111111234",
            CvcHash = "hashed-cvc",
            Limit = limit,
            Debt = debt,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = status,
            AssignedByUserId = "admin-1"
        };

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow =>
            new(2026, 8, 8, 14, 30, 0, TimeSpan.Zero);

        public DateTimeOffset Now =>
            new(2026, 8, 8, 10, 30, 0, TimeSpan.FromHours(-4));

        public DateOnly Today => new(2026, 8, 8);
    }

    private sealed class StubCreditCardRepository : ICreditCardRepository
    {
        public CreditCard? CardForUpdate { get; init; }

        public Guid? ReceivedCreditCardId { get; private set; }

        public Task<bool> ClientExistsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> GetForUpdateAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default)
        {
            ReceivedCreditCardId = creditCardId;
            return Task.FromResult(CardForUpdate);
        }

        public Task<CreditCard?> GetByCardNumberAsync(
            string cardNumber,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> CardNumberExistsAsync(
            string cardNumber,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddConsumptionAsync(
            CardConsumption consumption,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPaymentAsync(
            CardPayment payment,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CardPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => Task.FromResult<CardPayment?>(null);
        public Task<CardConsumption?> GetConsumptionByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => Task.FromResult<CardConsumption?>(null);
        public Task<IReadOnlyCollection<CreditCard>> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CreditCard>>(Array.Empty<CreditCard>());

        public Task<string?> FindClientIdByIdentificationAsync(
            string identification,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> HasAnyCardsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(
            int page,
            int pageSize,
            string? identification = null,
            CreditCardStatusFilter? status = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCardDetailReadModel?> GetDetailsAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(
            Guid creditCardId,
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetActiveDebtByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(
            IReadOnlyCollection<string> clientIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> IsActiveClientAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard> AddAsync(
            CreditCard entity,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<CreditCard>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IQueryable<CreditCard> GetAllQueryable(
            bool trackChanges = false) =>
            throw new NotImplementedException();

        public Task<CreditCard?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> UpdateAsync(
            Guid id,
            CreditCard value,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
