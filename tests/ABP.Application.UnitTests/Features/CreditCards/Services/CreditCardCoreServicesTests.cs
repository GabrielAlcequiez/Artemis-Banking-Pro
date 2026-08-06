using ABP.Application;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Interfaces;
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

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

public sealed class CreditCardCoreServicesTests
{
    [Fact]
    public async Task List_without_filters_returns_no_search_and_maps_safe_summary()
    {
        var repository = new FakeCreditCardRepository
        {
            Page = CreatePage(CreateSummary())
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(new CreditCardListRequest());

        Assert.Equal(CreditCardSearchStatus.NoSearch, result.SearchStatus);
        Assert.Single(result.Page.Data);
        Assert.Equal("************1234", result.Page.Data.Single().MaskedCardNumber);
        Assert.Equal("1234", result.Page.Data.Single().LastFourDigits);
        Assert.Equal("08/29", result.Page.Data.Single().ExpirationDate);
        Assert.Equal("Activa", result.Page.Data.Single().Status);
    }

    [Fact]
    public async Task List_with_unknown_identification_returns_client_not_found()
    {
        var repository = new FakeCreditCardRepository();
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(Identification: " 999 "));

        Assert.Equal(CreditCardSearchStatus.ClientNotFound, result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.False(repository.SearchWasCalled);
        Assert.Equal("999", repository.ReceivedIdentification);
    }

    [Fact]
    public async Task List_with_existing_client_without_cards_returns_client_without_cards()
    {
        var repository = new FakeCreditCardRepository
        {
            ClientIdByIdentification = "client-1",
            HasCards = false
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(Identification: "123"));

        Assert.Equal(CreditCardSearchStatus.ClientWithoutCards, result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.False(repository.SearchWasCalled);
    }

    [Fact]
    public async Task List_with_existing_client_and_no_matching_status_returns_no_matching_cards()
    {
        var repository = new FakeCreditCardRepository
        {
            ClientIdByIdentification = "client-1",
            HasCards = true,
            Page = CreatePage()
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(
                Identification: "123",
                Status: CreditCardStatusFilter.Cancelled));

        Assert.Equal(CreditCardSearchStatus.NoMatchingCards, result.SearchStatus);
        Assert.Empty(result.Page.Data);
    }

    [Fact]
    public async Task List_with_matching_filter_returns_results_found()
    {
        var repository = new FakeCreditCardRepository
        {
            Page = CreatePage(CreateSummary())
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(Status: CreditCardStatusFilter.All));

        Assert.Equal(CreditCardSearchStatus.ResultsFound, result.SearchStatus);
        Assert.Equal(1, result.Page.TotalRecords);
    }

    [Fact]
    public async Task GetDetail_maps_consumptions_without_sensitive_fields()
    {
        var cardId = Guid.NewGuid();
        var repository = new FakeCreditCardRepository
        {
            Detail = new CreditCardDetailReadModel(
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
                [new CardConsumptionReadModel(
                    Guid.NewGuid(),
                    new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero),
                    25m,
                    "AVANCE",
                    ConsumptionStatus.Approved)])
        };
        var service = CreateService(repository);

        var result = await service.GetDetailAsync(cardId);

        Assert.NotNull(result);
        Assert.Equal("************1234", result.MaskedCardNumber);
        Assert.Equal("08/29", result.ExpirationDate);
        Assert.Single(result.Consumptions);
        Assert.Equal("APROBADO", result.Consumptions.Single().Status);
    }

    [Fact]
    public async Task List_rejects_invalid_request_through_shared_validator()
    {
        var service = CreateService(new FakeCreditCardRepository());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ListAsync(new CreditCardListRequest(PageSize: 21)));
    }

    [Fact]
    public void Credit_card_profile_is_valid()
    {
        var provider = CreateProvider(new FakeCreditCardRepository());
        var mapper = provider.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Card_number_generator_returns_exactly_sixteen_digits()
    {
        var generator = new CardNumberGeneratorService();

        var cardNumber = generator.Generate();

        Assert.Equal(16, cardNumber.Length);
        Assert.All(cardNumber, character => Assert.InRange(character, '0', '9'));
    }

    [Fact]
    public async Task Card_debt_reader_delegates_to_active_debt_repository()
    {
        var repository = new FakeCreditCardRepository { ActiveDebt = 150.25m };
        var reader = new CardDebtReaderService(repository);

        var debt = await reader.GetActiveCardDebtByClientIdAsync("client-1");

        Assert.Equal(150.25m, debt);
    }

    private static ICreditCardService CreateService(FakeCreditCardRepository repository) =>
        CreateProvider(repository).GetRequiredService<ICreditCardService>();

    private static IServiceProvider CreateProvider(FakeCreditCardRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddApplicationServices();
        services.AddSingleton<ICreditCardRepository>(repository);
        return services.BuildServiceProvider();
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

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        public string? ClientIdByIdentification { get; init; }

        public bool HasCards { get; init; } = true;

        public PagedResult<CreditCardSummaryReadModel> Page { get; init; } = CreatePage();

        public CreditCardDetailReadModel? Detail { get; init; }

        public decimal ActiveDebt { get; init; }

        public bool SearchWasCalled { get; private set; }

        public string? ReceivedIdentification { get; private set; }

        public Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default) =>
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
            Task.FromResult(Detail);

        public Task<decimal> GetActiveDebtByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveDebt);

        public Task<CreditCard> AddAsync(CreditCard entity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<CreditCard>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IQueryable<CreditCard> GetAllQueryable(bool trackChanges = false) =>
            throw new NotImplementedException();

        public Task<CreditCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> UpdateAsync(Guid id, CreditCard value, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
