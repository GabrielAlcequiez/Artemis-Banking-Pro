using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Queries.GetCreditCards;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.CreditCards.Queries.GetCreditCards;

public sealed class GetCreditCardsQueryTests
{
    [Fact]
    public async Task Validator_reuses_shared_list_request_rules()
    {
        var validator = new GetCreditCardsQueryValidator(
            new CreditCardListRequestValidator());
        var query = new GetCreditCardsQuery(
            new CreditCardListRequest(PageSize: 21));

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.PageSize");
    }

    [Fact]
    public async Task Handler_without_filters_returns_no_search_and_maps_safe_summary()
    {
        var repository = new StubCreditCardRepository
        {
            Page = CreatePage(CreateSummary())
        };
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardsQuery(new CreditCardListRequest()),
            CancellationToken.None);

        Assert.Equal(CreditCardSearchStatus.NoSearch, result.SearchStatus);
        var card = Assert.Single(result.Page.Data);
        Assert.Equal("************1234", card.MaskedCardNumber);
        Assert.Equal("1234", card.LastFourDigits);
        Assert.Equal("08/29", card.ExpirationDate);
        Assert.Equal("Activa", card.Status);
    }

    [Fact]
    public async Task Handler_with_unknown_identification_returns_client_not_found()
    {
        var repository = new StubCreditCardRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardsQuery(
                new CreditCardListRequest(Identification: " 999 ")),
            CancellationToken.None);

        Assert.Equal(CreditCardSearchStatus.ClientNotFound, result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.False(repository.SearchWasCalled);
        Assert.Equal("999", repository.ReceivedIdentification);
    }

    [Fact]
    public async Task Handler_with_existing_client_without_cards_returns_client_without_cards()
    {
        var repository = new StubCreditCardRepository
        {
            ClientIdByIdentification = "client-1",
            HasCards = false
        };
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardsQuery(
                new CreditCardListRequest(Identification: "123")),
            CancellationToken.None);

        Assert.Equal(
            CreditCardSearchStatus.ClientWithoutCards,
            result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.False(repository.SearchWasCalled);
    }

    [Fact]
    public async Task Handler_with_existing_client_and_no_matches_returns_no_matching_cards()
    {
        var repository = new StubCreditCardRepository
        {
            ClientIdByIdentification = "client-1",
            HasCards = true,
            Page = CreatePage()
        };
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardsQuery(
                new CreditCardListRequest(
                    Identification: "123",
                    Status: CreditCardStatusFilter.Cancelled)),
            CancellationToken.None);

        Assert.Equal(
            CreditCardSearchStatus.NoMatchingCards,
            result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.True(repository.SearchWasCalled);
    }

    [Fact]
    public async Task Handler_with_matching_filter_returns_results_found()
    {
        var repository = new StubCreditCardRepository
        {
            Page = CreatePage(CreateSummary())
        };
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new GetCreditCardsQuery(
                new CreditCardListRequest(Status: CreditCardStatusFilter.All)),
            CancellationToken.None);

        Assert.Equal(CreditCardSearchStatus.ResultsFound, result.SearchStatus);
        Assert.Equal(1, result.Page.TotalRecords);
    }

    private static GetCreditCardsQueryHandler CreateHandler(
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

    private static PagedResult<CreditCardSummaryReadModel> CreatePage(
        params CreditCardSummaryReadModel[] data) =>
        new(data, 1, 20, data.Length);

    private static CreditCardSummaryReadModel CreateSummary() =>
        new(
            Guid.NewGuid(),
            "************1234",
            "1234",
            "client-1",
            "María Gómez",
            500m,
            350m,
            150m,
            new DateOnly(2029, 8, 31),
            CreditCardStatus.Active,
            new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero));

    private sealed class StubCreditCardRepository : ICreditCardRepository
    {
        public string? ClientIdByIdentification { get; init; }

        public bool HasCards { get; init; } = true;

        public PagedResult<CreditCardSummaryReadModel> Page { get; init; } =
            CreatePage();

        public bool SearchWasCalled { get; private set; }

        public string? ReceivedIdentification { get; private set; }

        public Task<bool> ClientExistsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string?> FindClientIdByIdentificationAsync(
            string identification,
            CancellationToken cancellationToken = default)
        {
            ReceivedIdentification = identification;
            return Task.FromResult(ClientIdByIdentification);
        }

        public Task<bool> HasAnyCardsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HasCards);

        public Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(
            int page,
            int pageSize,
            string? identification = null,
            CreditCardStatusFilter? status = null,
            CancellationToken cancellationToken = default)
        {
            SearchWasCalled = true;
            return Task.FromResult(Page);
        }

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

        public Task<CreditCard?> GetForUpdateAsync(
            Guid creditCardId,
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
