using System.Reflection;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClientLoansController = ABP.WebApp.Areas.Client.Controllers.LoansController;

namespace ABP.WebApp.IntegrationTests;

public sealed class ClientLoansControllerTests
{
    [Fact]
    public void Controller_declares_client_area_and_role()
    {
        var type = typeof(ClientLoansController);

        Assert.Equal(
            "Client",
            type.GetCustomAttribute<AreaAttribute>()?.RouteValue);
        Assert.Equal(
            nameof(Roles.Client),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }

    [Fact]
    public async Task Details_returns_the_authenticated_clients_loan()
    {
        var detail = CreateDetail();
        var service = new FakeLoanService { ClientDetail = detail };
        var controller = new ClientLoansController(service);

        var result = await controller.Details(
            detail.Id,
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoanDetailViewModel>(view.Model);
        Assert.Same(detail, model.Loan);
        Assert.Equal(detail.Id, service.ReceivedClientLoanId);
    }

    [Fact]
    public async Task Details_returns_not_found_when_loan_is_missing_or_not_owned()
    {
        var controller = new ClientLoansController(new FakeLoanService());

        var result = await controller.Details(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static LoanDetailDto CreateDetail() =>
        new(
            Guid.NewGuid(),
            "123456789",
            "client-1",
            "Ana Pérez",
            10_000m,
            12m,
            12,
            888.49m,
            10_661.88m,
            "Activo",
            "Al día",
            new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero),
            []);

    private sealed class FakeLoanService : ILoanService
    {
        public LoanDetailDto? ClientDetail { get; init; }

        public Guid? ReceivedClientLoanId { get; private set; }

        public Task<LoanDetailDto?> GetClientDetailAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
        {
            ReceivedClientLoanId = loanId;
            return Task.FromResult(ClientDetail);
        }

        public Task<PagedResult<LoanSummaryDto>> ListAsync(
            LoanListRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LoanDetailDto?> GetDetailAsync(
            Guid loanId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ClientLoanPortfolioItemDto?> GetClientActiveLoanAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
