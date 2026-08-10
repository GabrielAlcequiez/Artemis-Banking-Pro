using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Common;
using FluentValidation;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

public sealed class CreditCardClientSelectionServiceTests
{
    [Fact]
    public async Task Search_returns_only_reader_clients_with_total_and_average_debt()
    {
        var reader = new FakeActiveClientReader
        {
            Page = new PagedResult<ActiveClientSummaryDto>(
                [
                    new("client-1", "00100000001", "Ana Pérez", "ana@example.com"),
                    new("client-2", "00100000002", "Luis Díaz", "luis@example.com")
                ],
                2,
                2,
                8)
        };
        var debts = new FakeDebtSnapshotReader
        {
            AverageDebt = 275m,
            Debts =
            {
                ["client-1"] = 150m,
                ["client-2"] = 400m
            }
        };
        var service = CreateService(reader, debts);

        var result = await service.SearchAsync(
            new CreditCardClientSearchRequest(2, 2, " 001 "));

        Assert.Equal("001", reader.ReceivedRequest?.Identification);
        Assert.Equal(2, result.Page.Page);
        Assert.Equal(8, result.Page.TotalRecords);
        Assert.Equal(275m, result.AverageDebt);
        Assert.Collection(
            result.Page.Data,
            client => Assert.Equal(150m, client.TotalDebt),
            client => Assert.Equal(400m, client.TotalDebt));
    }

    [Fact]
    public async Task GetActiveClient_returns_client_with_total_debt()
    {
        var reader = new FakeActiveClientReader
        {
            Client = new("client-1", "00100000001", "Ana Pérez", "ana@example.com")
        };
        var debts = new FakeDebtSnapshotReader
        {
            Debts = { ["client-1"] = 925.50m }
        };
        var service = CreateService(reader, debts);

        var result = await service.GetActiveClientAsync("client-1");

        Assert.NotNull(result);
        Assert.Equal("client-1", reader.ReceivedClientId);
        Assert.Equal(925.50m, result.TotalDebt);
    }

    [Fact]
    public async Task GetActiveClient_when_reader_does_not_find_active_client_returns_null()
    {
        var service = CreateService(
            new FakeActiveClientReader(),
            new FakeDebtSnapshotReader());

        var result = await service.GetActiveClientAsync("missing-client");

        Assert.Null(result);
    }

    [Fact]
    public async Task Search_with_invalid_page_does_not_call_external_reader()
    {
        var reader = new FakeActiveClientReader();
        var service = CreateService(reader, new FakeDebtSnapshotReader());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SearchAsync(new CreditCardClientSearchRequest(0, 20)));

        Assert.Null(reader.ReceivedRequest);
    }

    private static CreditCardClientSelectionService CreateService(
        IActiveClientReader reader,
        ICustomerDebtSnapshotReader debts) =>
        new(reader, debts, new CreditCardClientSearchRequestValidator());

    private sealed class FakeActiveClientReader : IActiveClientReader
    {
        public PagedResult<ActiveClientSummaryDto> Page { get; init; } =
            new(Array.Empty<ActiveClientSummaryDto>(), 1, 20, 0);

        public ActiveClientSummaryDto? Client { get; init; }

        public CreditCardClientSearchRequest? ReceivedRequest { get; private set; }

        public string? ReceivedClientId { get; private set; }

        public Task<PagedResult<ActiveClientSummaryDto>> SearchAsync(
            CreditCardClientSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            return Task.FromResult(Page);
        }

        public Task<ActiveClientSummaryDto?> GetByIdAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            ReceivedClientId = clientId;
            return Task.FromResult(Client);
        }
    }

    private sealed class FakeDebtSnapshotReader : ICustomerDebtSnapshotReader
    {
        public Dictionary<string, decimal> Debts { get; } = [];

        public decimal AverageDebt { get; init; }

        public Task<decimal> GetTotalDebtAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Debts.GetValueOrDefault(clientId));

        public Task<decimal> GetAverageActiveClientDebtAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AverageDebt);
    }
}
