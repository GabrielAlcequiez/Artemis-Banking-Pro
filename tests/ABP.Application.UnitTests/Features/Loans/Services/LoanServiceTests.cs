using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Mapping;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class LoanServiceTests
{
    private readonly IMapper mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<LoanProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task List_normalizes_identification_and_maps_page()
    {
        var repository = new FakeLoanRepository
        {
            Page = new PagedResult<LoanSummaryReadModel>([CreateLoanSummary()], 2, 5, 6)
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new LoanListRequest(2, 5, " 00123456789 ", LoanStatusFilter.Active));

        Assert.Single(result.Data);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(6, result.TotalRecords);
        Assert.Equal("00123456789", repository.ReceivedIdentification);
        Assert.Equal(LoanStatusFilter.Active, repository.ReceivedStatus);
        Assert.Equal("123456789", result.Data.Single().LoanNumber);
        Assert.Equal("Ana Pérez", result.Data.Single().ClientFullName);
        Assert.Equal(12, result.Data.Single().TotalInstallments);
        Assert.Equal(4, result.Data.Single().PaidInstallments);
    }

    [Fact]
    public async Task List_converts_blank_identification_to_null()
    {
        var repository = new FakeLoanRepository();
        var service = CreateService(repository);

        await service.ListAsync(new LoanListRequest(Identification: "   "));

        Assert.Null(repository.ReceivedIdentification);
    }

    [Fact]
    public async Task List_rejects_invalid_request_before_querying_repository()
    {
        var repository = new FakeLoanRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ListAsync(new LoanListRequest(PageSize: 21)));

        Assert.Equal(0, repository.GetPagedCalls);
    }

    [Fact]
    public async Task Get_detail_maps_loan_and_amortization()
    {
        var loan = CreateLoan();
        loan.Installments =
        [
            new LoanInstallment
            {
                Number = 1,
                DueDate = new DateOnly(2026, 9, 10),
                InstallmentAmount = 100m,
                InterestAmount = 10m,
                CapitalAmount = 90m,
                PendingAmount = 100m,
                PaymentStatus = InstallmentPaymentStatus.Pending
            }
        ];
        var repository = new FakeLoanRepository { Detail = loan };
        var service = CreateService(repository);

        var result = await service.GetDetailAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal("Ana Pérez", result.ClientFullName);
        Assert.Equal(100m, result.MonthlyInstallment);
        Assert.Single(result.Amortization);
        Assert.Equal("Pendiente", result.Amortization.Single().PaymentStatus);
    }

    [Fact]
    public async Task Get_detail_returns_null_when_loan_does_not_exist()
    {
        var service = CreateService(new FakeLoanRepository());

        var result = await service.GetDetailAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_client_detail_filters_by_authenticated_client_and_maps_loan()
    {
        var loan = CreateLoan();
        var repository = new FakeLoanRepository { ClientDetail = loan };
        var currentUser = new FakeCurrentUserService
        {
            UserId = "client-1"
        };
        var service = CreateService(repository, currentUser);

        var result = await service.GetClientDetailAsync(loan.Id);

        Assert.NotNull(result);
        Assert.Equal(loan.Id, repository.ReceivedClientLoanId);
        Assert.Equal("client-1", repository.ReceivedClientId);
        Assert.Equal("123456789", result.LoanNumber);
    }

    [Fact]
    public async Task Get_client_detail_does_not_query_when_user_is_unauthenticated()
    {
        var repository = new FakeLoanRepository { ClientDetail = CreateLoan() };
        var service = CreateService(
            repository,
            new FakeCurrentUserService { IsAuthenticated = false });

        var result = await service.GetClientDetailAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Equal(0, repository.GetClientDetailsCalls);
    }

    [Fact]
    public async Task Get_client_detail_does_not_query_when_user_is_not_a_client()
    {
        var repository = new FakeLoanRepository { ClientDetail = CreateLoan() };
        var service = CreateService(
            repository,
            new FakeCurrentUserService
            {
                UserRoles = [nameof(Roles.Administrator)]
            });

        var result = await service.GetClientDetailAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Equal(0, repository.GetClientDetailsCalls);
    }

    [Fact]
    public async Task Get_client_active_loan_filters_by_user_and_maps_portfolio_item()
    {
        var loanId = Guid.NewGuid();
        var repository = new FakeLoanRepository
        {
            ActivePortfolio = new ClientLoanPortfolioReadModel(
                loanId,
                "123456789",
                10_000m,
                7_500m,
                900m,
                12m,
                12,
                12,
                3,
                false)
        };
        var service = CreateService(repository);

        var result = await service.GetClientActiveLoanAsync();

        Assert.NotNull(result);
        Assert.Equal("client-1", repository.ReceivedPortfolioClientId);
        Assert.Equal(loanId, result.Id);
        Assert.Equal(7_500m, result.PendingAmount);
        Assert.Equal(900m, result.MonthlyInstallment);
    }

    [Fact]
    public async Task Get_client_active_loan_does_not_query_for_unauthenticated_user()
    {
        var repository = new FakeLoanRepository();
        var service = CreateService(
            repository,
            new FakeCurrentUserService { IsAuthenticated = false });

        var result = await service.GetClientActiveLoanAsync();

        Assert.Null(result);
        Assert.Equal(0, repository.GetActivePortfolioCalls);
    }

    private ILoanService CreateService(
        FakeLoanRepository repository,
        FakeCurrentUserService? currentUser = null) =>
        new LoanService(
            repository,
            mapper,
            new LoanListRequestValidator(),
            currentUser ?? new FakeCurrentUserService());

    private static Loan CreateLoan() => new()
    {
        ClientId = "client-1",
        Client = new User("client-1")
        {
            Name = "Ana",
            LastName = "Pérez"
        },
        LoanNumber = "123456789",
        Capital = 1_000m,
        PendingAmount = 1_000m,
        AnnualInterestRate = 12m,
        TermInMonths = 12,
        Status = LoanStatus.Active,
        AssignedByUserId = "admin-1"
    };

    private static LoanSummaryReadModel CreateLoanSummary() => new(
        Guid.NewGuid(),
        "123456789",
        "client-1",
        "Ana Pérez",
        1_000m,
        12,
        4,
        1_000m,
        12m,
        12,
        LoanStatus.Active,
        false,
        new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));

    private sealed class FakeLoanRepository : ILoanRepository
    {
        public PagedResult<LoanSummaryReadModel> Page { get; init; } = new([], 1, 20, 0);

        public Loan? Detail { get; init; }

        public Loan? ClientDetail { get; init; }

        public ClientLoanPortfolioReadModel? ActivePortfolio { get; init; }

        public int GetPagedCalls { get; private set; }

        public string? ReceivedIdentification { get; private set; }

        public LoanStatusFilter? ReceivedStatus { get; private set; }

        public Guid? ReceivedClientLoanId { get; private set; }

        public string? ReceivedClientId { get; private set; }

        public int GetClientDetailsCalls { get; private set; }

        public string? ReceivedPortfolioClientId { get; private set; }

        public int GetActivePortfolioCalls { get; private set; }

        public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(
            PagedRequest request,
            string? clientIdentification = null,
            LoanStatusFilter? status = null,
            CancellationToken cancellationToken = default)
        {
            GetPagedCalls++;
            ReceivedIdentification = clientIdentification;
            ReceivedStatus = status;
            return Task.FromResult(Page);
        }

        public Task<Loan?> GetWithInstallmentsAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> GetDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<Loan?> GetDetailsForClientAsync(
            Guid id,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            GetClientDetailsCalls++;
            ReceivedClientLoanId = id;
            ReceivedClientId = clientId;
            return Task.FromResult(ClientDetail);
        }

        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            GetActivePortfolioCalls++;
            ReceivedPortfolioClientId = clientId;
            return Task.FromResult(ActivePortfolio);
        }

        public Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(PagedRequest request, string? clientIdentification = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> MarkOverdueInstallmentsAsync(DateOnly bankingDate, DateTimeOffset modifiedAtUtc, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> ClearLateFlagFromPaidInstallmentsAsync(Guid? loanId, DateTimeOffset modifiedAtUtc, string? modifiedByUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Loan>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan> AddAsync(Loan entity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> UpdateAsync(Guid id, Loan value, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated { get; init; } = true;

        public string? UserId { get; init; } = "client-1";

        public string? UserName => null;

        public Guid? CommerceId => null;

        public IReadOnlyCollection<string> UserRoles { get; init; } =
            [nameof(ABP.Domain.Enums.Roles.Client)];

        public IReadOnlyCollection<string> Roles => UserRoles;

        public bool IsInRole(string role) =>
            UserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
