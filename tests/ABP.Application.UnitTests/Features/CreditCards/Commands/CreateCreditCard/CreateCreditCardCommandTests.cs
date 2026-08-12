using System.Data;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.Commands.CreateCreditCard;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;

namespace ABP.Application.UnitTests.Features.CreditCards.Commands.CreateCreditCard;

public sealed class CreateCreditCardCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_create_request_rules()
    {
        var validator = new CreateCreditCardCommandValidator(
            new CreateCreditCardRequestValidator());
        var command = new CreateCreditCardCommand(
            new CreateCreditCardRequest(string.Empty, 0m));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.ClientId");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.CreditLimit");
    }

    [Fact]
    public async Task Handler_assigns_active_card_and_commits_once()
    {
        var repository = new StubCreditCardRepository
        {
            IsActiveClient = true,
            CardNumberExists = false
        };
        var unitOfWork = new StubUnitOfWork();
        var cvcService = new StubCvcService("007", "hashed-007");
        var transaction = new StubFinancialTransaction();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            cvcService: cvcService,
            numberGenerator: new StubCardNumberGenerator("0000000000001234"),
            clock: new StubClock(new DateOnly(2026, 8, 8)),
            currentUser: StubCurrentUser.Administrator("admin-1"),
            transaction: transaction);

        var result = await handler.Handle(
            new CreateCreditCardCommand(
                new CreateCreditCardRequest("client-1", 5_000m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var card = Assert.IsType<CreditCard>(repository.AddedCard);
        Assert.Equal(card.Id, result.Value);
        Assert.Equal("client-1", card.ClientId);
        Assert.Equal("0000000000001234", card.CardNumber);
        Assert.Equal("hashed-007", card.CvcHash);
        Assert.Equal("007", cvcService.LastHashedCvc);
        Assert.NotEqual(cvcService.GeneratedCvc, card.CvcHash);
        Assert.Equal(5_000m, card.Limit);
        Assert.Equal(0m, card.Debt);
        Assert.Equal(new DateOnly(2029, 8, 31), card.ExpirationDate);
        Assert.Equal(CreditCardStatus.Active, card.Status);
        Assert.Equal("admin-1", card.AssignedByUserId);
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Fact]
    public async Task Handler_rejects_inactive_client_without_generating_or_committing()
    {
        var repository = new StubCreditCardRepository
        {
            IsActiveClient = false
        };
        var unitOfWork = new StubUnitOfWork();
        var numberGenerator = new StubCardNumberGenerator();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            numberGenerator: numberGenerator);

        var result = await handler.Handle(
            ValidCommand(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.ClientInactive, result.Error);
        Assert.Equal(0, numberGenerator.GenerateCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_distinguishes_missing_client_from_inactive_client()
    {
        var repository = new StubCreditCardRepository
        {
            ClientExists = false,
            IsActiveClient = false
        };
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.ClientNotFound, result.Error);
        Assert.Equal(0, repository.IsActiveClientCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_requires_authenticated_administrator()
    {
        var repository = new StubCreditCardRepository
        {
            IsActiveClient = true
        };
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            currentUser: StubCurrentUser.Client("client-1"));

        var result = await handler.Handle(
            ValidCommand(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.AdministratorRequired, result.Error);
        Assert.Equal(0, repository.IsActiveClientCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_returns_generation_failure_after_ten_collisions()
    {
        var repository = new StubCreditCardRepository
        {
            IsActiveClient = true,
            CardNumberExists = true
        };
        var unitOfWork = new StubUnitOfWork();
        var numberGenerator = new StubCardNumberGenerator();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            numberGenerator: numberGenerator);

        var result = await handler.Handle(
            ValidCommand(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.NumberGenerationFailed, result.Error);
        Assert.Equal(10, numberGenerator.GenerateCalls);
        Assert.Equal(10, repository.CardNumberExistsCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    private static CreateCreditCardCommandHandler CreateHandler(
        StubCreditCardRepository repository,
        StubUnitOfWork? unitOfWork = null,
        ICvcService? cvcService = null,
        ICardNumberGeneratorService? numberGenerator = null,
        IClock? clock = null,
        ICurrentUserService? currentUser = null,
        StubFinancialTransaction? transaction = null) =>
        new(
            cvcService ?? new StubCvcService(),
            numberGenerator ?? new StubCardNumberGenerator(),
            repository,
            unitOfWork ?? new StubUnitOfWork(),
            transaction ?? new StubFinancialTransaction(),
            clock ?? new StubClock(new DateOnly(2026, 8, 8)),
            currentUser ?? StubCurrentUser.Administrator("admin-1"));

    private static CreateCreditCardCommand ValidCommand() =>
        new(new CreateCreditCardRequest("client-1", 5_000m));

    private sealed class StubFinancialTransaction : IFinancialTransaction
    {
        public IsolationLevel? IsolationLevel { get; private set; }

        public Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<TResult> ExecuteAsync<TResult>(
            IsolationLevel isolationLevel,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            IsolationLevel = isolationLevel;
            return operation(cancellationToken);
        }
    }

    private sealed class StubCvcService(
        string generatedCvc = "123",
        string hashedCvc = "hashed-123") : ICvcService
    {
        public string GeneratedCvc { get; } = generatedCvc;

        public string? LastHashedCvc { get; private set; }

        public string Generate() => GeneratedCvc;

        public string Hash(string cvc)
        {
            LastHashedCvc = cvc;
            return hashedCvc;
        }

        public bool Verify(string cvc, string cvcHash) =>
            cvc == GeneratedCvc && cvcHash == hashedCvc;
    }

    private sealed class StubCardNumberGenerator(
        string cardNumber = "0000000000001234")
        : ICardNumberGeneratorService
    {
        public int GenerateCalls { get; private set; }

        public string Generate()
        {
            GenerateCalls++;
            return cardNumber;
        }
    }

    private sealed class StubClock(DateOnly today) : IClock
    {
        public DateTimeOffset UtcNow =>
            new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => today;
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        private StubCurrentUser(
            bool isAuthenticated,
            string? userId,
            IReadOnlyCollection<string> roles)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
            Roles = roles;
        }

        public bool IsAuthenticated { get; }

        public string? UserId { get; }

        public string? UserName => null;

        public Guid? CommerceId => null;

        public IReadOnlyCollection<string> Roles { get; }

        public bool IsInRole(string role) => Roles.Contains(role);

        public static StubCurrentUser Administrator(string userId) =>
            new(true, userId, [ABP.Domain.Enums.Roles.Administrator.ToString()]);

        public static StubCurrentUser Client(string userId) =>
            new(true, userId, [ABP.Domain.Enums.Roles.Client.ToString()]);
    }

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

    private sealed class StubCreditCardRepository : ICreditCardRepository
    {
        public bool ClientExists { get; init; } = true;

        public bool IsActiveClient { get; init; }

        public bool CardNumberExists { get; init; }

        public int IsActiveClientCalls { get; private set; }

        public int CardNumberExistsCalls { get; private set; }

        public int AddCalls { get; private set; }

        public CreditCard? AddedCard { get; private set; }

        public Task<bool> ClientExistsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ClientExists);

        public Task<bool> IsActiveClientAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            IsActiveClientCalls++;
            return Task.FromResult(IsActiveClient);
        }

        public Task<bool> CardNumberExistsAsync(
            string cardNumber,
            CancellationToken cancellationToken = default)
        {
            CardNumberExistsCalls++;
            return Task.FromResult(CardNumberExists);
        }

        public Task<CreditCard> AddAsync(
            CreditCard entity,
            CancellationToken cancellationToken = default)
        {
            AddCalls++;
            AddedCard = entity;
            return Task.FromResult(entity);
        }

        public Task<CreditCard?> GetByCardNumberAsync(
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

        public Task<CreditCard?> GetForUpdateAsync(
            Guid creditCardId,
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
