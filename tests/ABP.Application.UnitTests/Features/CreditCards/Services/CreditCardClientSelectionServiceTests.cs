using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using FluentValidation;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

public sealed class CreditCardClientSelectionServiceTests
{
    [Fact]
    public async Task Search_returns_repository_clients_with_total_and_average_debt()
    {
        var repository = new FakeUserRepository
        {
            Page = new PagedResult<User>(
                [
                    CreateActiveClient("client-1", "00100000001", "Ana", "Pérez", "ana@example.com"),
                    CreateActiveClient("client-2", "00100000002", "Luis", "Díaz", "luis@example.com")
                ],
                2,
                2,
                8)
        };
        var debts = new FakeCustomerDebtService
        {
            AverageDebt = 275m,
            Debts =
            {
                ["client-1"] = 150m,
                ["client-2"] = 400m
            }
        };
        var service = CreateService(repository, debts);

        var result = await service.SearchAsync(
            new CreditCardClientSearchRequest(2, 2, " 001 "));

        Assert.Equal("001", repository.ReceivedIdentification);
        Assert.Equal(2, result.Page.Page);
        Assert.Equal(8, result.Page.TotalRecords);
        Assert.Equal(275m, result.AverageDebt);
        Assert.Collection(
            result.Page.Data,
            client =>
            {
                Assert.Equal("Ana Pérez", client.FullName);
                Assert.Equal(150m, client.TotalDebt);
            },
            client => Assert.Equal(400m, client.TotalDebt));
    }

    [Fact]
    public async Task GetActiveClient_returns_client_with_total_debt()
    {
        var repository = new FakeUserRepository
        {
            Client = CreateActiveClient(
                "client-1",
                "00100000001",
                "Ana",
                "Pérez",
                "ana@example.com")
        };
        var debts = new FakeCustomerDebtService
        {
            Debts = { ["client-1"] = 925.50m }
        };
        var service = CreateService(repository, debts);

        var result = await service.GetActiveClientAsync("client-1");

        Assert.NotNull(result);
        Assert.Equal("client-1", repository.ReceivedClientId);
        Assert.Equal(925.50m, result.TotalDebt);
    }

    [Fact]
    public async Task GetActiveClient_when_repository_does_not_find_active_client_returns_null()
    {
        var service = CreateService(
            new FakeUserRepository(),
            new FakeCustomerDebtService());

        var result = await service.GetActiveClientAsync("missing-client");

        Assert.Null(result);
    }

    [Fact]
    public async Task Search_with_invalid_page_does_not_call_repository()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, new FakeCustomerDebtService());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SearchAsync(new CreditCardClientSearchRequest(0, 20)));

        Assert.Null(repository.ReceivedRequest);
    }

    private static CreditCardClientSelectionService CreateService(
        IUserRepository repository,
        ICustomerDebtService debts) =>
        new(repository, debts, new CreditCardClientSearchRequestValidator());

    private static User CreateActiveClient(
        string id,
        string identification,
        string name,
        string lastName,
        string email) =>
        new(id)
        {
            Identification = identification,
            Name = name,
            LastName = lastName,
            Email = email,
            Role = Roles.Client,
            IsActive = true
        };

    private sealed class FakeUserRepository : IUserRepository
    {
        public PagedResult<User> Page { get; init; } =
            new(Array.Empty<User>(), 1, 20, 0);

        public User? Client { get; init; }

        public PagedRequest? ReceivedRequest { get; private set; }

        public string? ReceivedIdentification { get; private set; }

        public string? ReceivedClientId { get; private set; }

        public Task<PagedResult<User>> GetActiveClientsPagedAsync(
            PagedRequest request,
            string? identification = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedIdentification = identification;
            return Task.FromResult(Page);
        }

        public Task<User?> GetActiveClientByIdAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            ReceivedClientId = clientId;
            return Task.FromResult(Client);
        }

        public Task<int> CountActiveClientsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Page.TotalRecords);

        public Task<User?> FindByIdentificationAsync(string identification) =>
            Task.FromResult<User?>(null);

        public Task<PagedResult<User>> GetPagedAsync(
            PagedRequest request,
            bool commerceOnly = false,
            Roles? role = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Page);

        public IQueryable<User> GetAllQueryable(bool trackChanges = false) =>
            Page.Data.AsQueryable();

        public Task<User?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Client);

        public Task<IReadOnlyList<User>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(Page.Data.ToList());

        public Task<User> AddAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task<User?> UpdateAsync(
            string id,
            User value,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(value);

        public Task<User?> DeleteAsync(
            string id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
    }

    private sealed class FakeCustomerDebtService : ICustomerDebtService
    {
        public Dictionary<string, decimal> Debts { get; init; } = [];

        public decimal AverageDebt { get; init; }

        public Task<decimal> GetTotalDebtAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Debts.GetValueOrDefault(clientId));

        public Task<IReadOnlyDictionary<string, decimal>> GetTotalDebtsAsync(
            IReadOnlyCollection<string> clientIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, decimal> result = clientIds.ToDictionary(
                clientId => clientId,
                clientId => Debts.GetValueOrDefault(clientId));
            return Task.FromResult(result);
        }

        public Task<decimal> GetAverageActiveClientDebtAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AverageDebt);
    }
}
