using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using FluentValidation;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class LoanClientSelectionServiceTests
{
    [Fact]
    public async Task Search_returns_eligible_clients_with_current_and_average_debt()
    {
        var repository = new FakeLoanRepository
        {
            EligibleClientsPage = new PagedResult<LoanClientCandidateReadModel>(
                [
                    CreateCandidate("client-1", "00100000001", "Ana Pérez", "ana@example.com"),
                    CreateCandidate("client-2", "00100000002", "Luis Díaz", "luis@example.com")
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
            new LoanClientSearchRequest(2, 2, " 001 "));

        Assert.Equal("001", repository.ReceivedIdentification);
        Assert.Equal(2, repository.ReceivedRequest?.Page);
        Assert.Equal(2, repository.ReceivedRequest?.PageSize);
        Assert.Equal(2, result.Page.Page);
        Assert.Equal(8, result.Page.TotalRecords);
        Assert.Equal(275m, result.AverageDebt);
        Assert.Collection(
            result.Page.Data,
            client =>
            {
                Assert.Equal("Ana Pérez", client.FullName);
                Assert.Equal(150m, client.CurrentDebt);
            },
            client => Assert.Equal(400m, client.CurrentDebt));
    }

    [Fact]
    public async Task GetEligibleClient_returns_client_with_current_debt()
    {
        var repository = new FakeLoanRepository
        {
            EligibleClient = CreateCandidate(
                "client-1",
                "00100000001",
                "Ana Pérez",
                "ana@example.com")
        };
        var debts = new FakeCustomerDebtService
        {
            Debts = { ["client-1"] = 925.50m }
        };
        var service = CreateService(repository, debts);

        var result = await service.GetEligibleClientAsync("client-1");

        Assert.NotNull(result);
        Assert.Equal("client-1", repository.ReceivedClientId);
        Assert.Equal(925.50m, result.CurrentDebt);
    }

    [Fact]
    public async Task GetEligibleClient_returns_null_when_client_is_not_eligible()
    {
        var service = CreateService(
            new FakeLoanRepository(),
            new FakeCustomerDebtService());

        var result = await service.GetEligibleClientAsync("missing-client");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetEligibleClient_does_not_query_repository_for_blank_id(
        string clientId)
    {
        var repository = new FakeLoanRepository();
        var service = CreateService(repository, new FakeCustomerDebtService());

        var result = await service.GetEligibleClientAsync(clientId);

        Assert.Null(result);
        Assert.Equal(0, repository.GetEligibleClientCalls);
    }

    [Fact]
    public async Task Search_with_invalid_page_does_not_query_repository()
    {
        var repository = new FakeLoanRepository();
        var service = CreateService(repository, new FakeCustomerDebtService());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SearchAsync(new LoanClientSearchRequest(Page: 0)));

        Assert.Null(repository.ReceivedRequest);
    }

    private static LoanClientSelectionService CreateService(
        ILoanRepository repository,
        ICustomerDebtService debts) =>
        new(repository, debts, new LoanClientSearchRequestValidator());

    private static LoanClientCandidateReadModel CreateCandidate(
        string id,
        string identification,
        string fullName,
        string email) =>
        new(id, identification, fullName, email);

    private sealed class FakeLoanRepository : ILoanRepository
    {
        public PagedResult<LoanClientCandidateReadModel> EligibleClientsPage { get; init; } =
            new([], 1, 20, 0);

        public LoanClientCandidateReadModel? EligibleClient { get; init; }

        public PagedRequest? ReceivedRequest { get; private set; }

        public string? ReceivedIdentification { get; private set; }

        public string? ReceivedClientId { get; private set; }

        public int GetEligibleClientCalls { get; private set; }

        public Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(
            PagedRequest request,
            string? clientIdentification = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedIdentification = clientIdentification;
            return Task.FromResult(EligibleClientsPage);
        }

        public Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            GetEligibleClientCalls++;
            ReceivedClientId = clientId;
            return Task.FromResult(EligibleClient);
        }

        public Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsForClientAsync(Guid id, string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatusFilter? status = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<LoanInstallment>> GetInstallmentsForDelinquencyUpdateAsync(DateOnly bankingDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<IReadOnlyList<Loan>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan> AddAsync(Loan entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> UpdateAsync(Guid id, Loan value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
