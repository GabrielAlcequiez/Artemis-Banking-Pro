using ABP.Application.Features.CreditCards.Queries.GetCreditCardDetail;
using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.CreditCards.Queries.GetCreditCardDetail;

public sealed class GetCreditCardDetailQueryTests
{
    [Fact]
    public async Task Handler_with_existing_card_maps_safe_detail_and_consumptions()
    {
        var cardId = Guid.NewGuid();
        var repository = new StubCreditCardRepository
        {
            Detail = CreateDetail(cardId)
        };
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardDetailQuery(cardId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(cardId, repository.ReceivedCreditCardId);
        Assert.Equal("************1234", result.MaskedCardNumber);
        Assert.Equal("1234", result.LastFourDigits);
        Assert.Equal("08/29", result.ExpirationDate);
        Assert.Equal("Activa", result.Status);

        var consumption = Assert.Single(result.Consumptions);
        Assert.Equal("AVANCE", consumption.CommerceName);
        Assert.Equal("APROBADO", consumption.Status);
    }

    [Fact]
    public async Task Handler_with_missing_card_returns_null()
    {
        var cardId = Guid.NewGuid();
        var repository = new StubCreditCardRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardDetailQuery(cardId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(cardId, repository.ReceivedCreditCardId);
    }

    private static GetCreditCardDetailQueryHandler CreateHandler(
        ICreditCardRepository repository) =>
        new(repository, CreateMapper());

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddApplicationServices();

        return services
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();
    }

    private static CreditCardDetailReadModel CreateDetail(Guid cardId) =>
        new(
            cardId,
            "************1234",
            "1234",
            "client-1",
            "María Gómez",
            500m,
            350m,
            150m,
            new DateOnly(2029, 8, 31),
            CreditCardStatus.Active,
            new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero),
            [new CardConsumptionReadModel(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero),
                25m,
                "AVANCE",
                ConsumptionStatus.Approved)]);

    private sealed class StubCreditCardRepository : ICreditCardRepository
    {
        public CreditCardDetailReadModel? Detail { get; init; }

        public Guid? ReceivedCreditCardId { get; private set; }

        public Task<bool> ClientExistsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCardDetailReadModel?> GetDetailsAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default)
        {
            ReceivedCreditCardId = creditCardId;
            return Task.FromResult(Detail);
        }

        public Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(
            Guid creditCardId,
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

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

        public Task<CreditCard?> GetForUpdateAsync(
            Guid creditCardId,
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
