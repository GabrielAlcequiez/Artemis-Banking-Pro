using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.Commands.CancelCreditCard;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;

namespace ABP.Application.UnitTests.Features.CreditCards.Commands.CancelCreditCard;

public sealed class CancelCreditCardCommandTests
{
    [Fact]
    public async Task Handle_cancels_active_debt_free_card_and_commits_once()
    {
        var cardId = Guid.NewGuid();
        var card = CreateCard(CreditCardStatus.Active, debt: 0m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelCreditCardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(cardId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cardId, repository.RequestedCreditCardId);
        Assert.Equal(CreditCardStatus.Cancelled, card.Status);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handle_rejects_card_with_outstanding_debt_without_changes_or_commit()
    {
        var card = CreateCard(CreditCardStatus.Active, debt: 0.01m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelCreditCardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.OutstandingDebt, result.Error);
        Assert.Equal(CreditCardStatus.Active, card.Status);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handle_rejects_already_cancelled_card_without_commit()
    {
        var card = CreateCard(CreditCardStatus.Cancelled, debt: 0m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelCreditCardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.Cancelled, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_commit()
    {
        var repository = new FakeCreditCardRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelCreditCardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Validator_reuses_shared_request_rules()
    {
        var validator = new CancelCreditCardCommandValidator(
            new CancelCreditCardRequestValidator());
        var command = CreateCommand(Guid.Empty);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.CreditCardId");
    }

    private static CancelCreditCardCommand CreateCommand(Guid creditCardId) =>
        new(new CancelCreditCardRequest(creditCardId));

    private static CreditCard CreateCard(
        CreditCardStatus status,
        decimal debt) =>
        new()
        {
            Status = status,
            Debt = debt,
            Limit = 500m
        };

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        public CreditCard? CardForUpdate { get; init; }

        public Guid? RequestedCreditCardId { get; private set; }

        public Task<bool> ClientExistsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> GetForUpdateAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default)
        {
            RequestedCreditCardId = creditCardId;
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

        public IQueryable<CreditCard> GetAllQueryable(
            bool trackChanges = false) =>
            throw new NotImplementedException();

        public Task<CreditCard?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<CreditCard>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard> AddAsync(
            CreditCard entity,
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
