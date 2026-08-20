using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Admin.Controllers;
using ABP.WebApp.Areas.Admin.ViewModels.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ABP.WebApp.IntegrationTests;

public sealed class LoansControllerTests
{
    [Fact]
    public void Controller_declares_admin_area_role_and_antiforgery_on_every_post()
    {
        var type = typeof(LoansController);

        Assert.Equal(
            "Admin",
            type.GetCustomAttribute<AreaAttribute>()?.RouteValue);
        Assert.Equal(
            nameof(Roles.Administrator),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);

        var postActions = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method =>
                method.GetCustomAttribute<HttpPostAttribute>() is not null)
            .ToArray();

        Assert.Equal(4, postActions.Length);
        Assert.All(
            postActions,
            method => Assert.NotNull(
                method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
    }

    [Fact]
    public async Task Index_translates_filters_and_returns_service_page()
    {
        var loans = new FakeLoanService
        {
            ListResult = EmptyLoanPage()
        };
        var controller = CreateController(
            loans,
            new FakeClientSelectionService(),
            new FakeOriginationService(),
            new FakeLoanRateService());

        var result = await controller.Index(
            2,
            10,
            "00100000001",
            "completado",
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoanIndexViewModel>(view.Model);
        Assert.Same(loans.ListResult, model.Result);
        Assert.Equal(LoanStatusFilter.Completed, loans.ReceivedListRequest?.Status);
        Assert.Equal(2, loans.ReceivedListRequest?.Page);
        Assert.Equal(10, loans.ReceivedListRequest?.PageSize);
    }

    [Fact]
    public async Task SelectClient_get_returns_eligible_clients_and_average_debt()
    {
        var selection = new FakeClientSelectionService
        {
            SearchResult = ClientPage()
        };
        var controller = CreateController(
            new FakeLoanService(),
            selection,
            new FakeOriginationService(),
            new FakeLoanRateService());

        var result = await controller.SelectClient(
            1,
            20,
            " 001 ",
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoanClientSelectionViewModel>(view.Model);
        Assert.Same(selection.SearchResult, model.Result);
        Assert.Equal(" 001 ", selection.ReceivedSearchRequest?.Identification);
        Assert.Equal(350m, model.Result?.Page.Data.Single().CurrentDebt);
        Assert.Equal(225m, model.Result?.AverageDebt);
    }

    [Fact]
    public async Task Create_post_with_high_risk_returns_warning_without_originating()
    {
        var client = CreateClient();
        var origination = new FakeOriginationService
        {
            AssessmentResult = OperationResult<HighRiskAssessmentDto>.Success(
                new HighRiskAssessmentDto(
                    LoanRiskType.ProjectedHighRisk.ToString(),
                    350m,
                    11_011.88m,
                    225m,
                    true))
        };
        var controller = CreateController(
            new FakeLoanService(),
            new FakeClientSelectionService { Client = client },
            origination,
            new FakeLoanRateService());

        var result = await controller.Create(
            new CreateLoanViewModel
            {
                ClientId = client.Id,
                CapitalAmount = 10_000m,
                TermInMonths = 12,
                AnnualInterestRate = 12m
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(LoansController.RiskWarning), view.ViewName);
        var warning = Assert.IsType<LoanRiskWarningViewModel>(view.Model);
        Assert.Equal(LoanRiskType.ProjectedHighRisk.ToString(), warning.RiskType);
        Assert.Equal(11_011.88m, warning.ProjectedDebt);
        Assert.Equal(1, origination.AssessCalls);
        Assert.Equal(0, origination.CreateCalls);
    }

    [Fact]
    public async Task Create_post_without_risk_uses_prg_and_temp_data()
    {
        var client = CreateClient();
        var detail = CreateDetail();
        var origination = new FakeOriginationService
        {
            AssessmentResult = OperationResult<HighRiskAssessmentDto>.Success(
                NoRiskAssessment()),
            CreateResult = OperationResult<LoanDetailDto>.Success(detail),
            HasNotificationWarning = true
        };
        var controller = CreateController(
            new FakeLoanService(),
            new FakeClientSelectionService { Client = client },
            origination,
            new FakeLoanRateService());

        var result = await controller.Create(
            new CreateLoanViewModel
            {
                ClientId = client.Id,
                CapitalAmount = 10_000m,
                TermInMonths = 12,
                AnnualInterestRate = 12m
            },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(LoansController.Details), redirect.ActionName);
        Assert.Equal(detail.Id, redirect.RouteValues?["id"]);
        Assert.False(origination.ReceivedCreateRequest?.ConfirmHighRisk);
        Assert.Equal(
            "El préstamo fue creado correctamente, pero no fue posible enviar el correo de notificación.",
            controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task ConfirmAssignment_reassesses_and_forces_explicit_confirmation()
    {
        var client = CreateClient();
        var detail = CreateDetail();
        var origination = new FakeOriginationService
        {
            AssessmentResult = OperationResult<HighRiskAssessmentDto>.Success(
                new HighRiskAssessmentDto(
                    LoanRiskType.CurrentHighRisk.ToString(),
                    500m,
                    11_161.88m,
                    225m,
                    true)),
            CreateResult = OperationResult<LoanDetailDto>.Success(detail)
        };
        var controller = CreateController(
            new FakeLoanService(),
            new FakeClientSelectionService { Client = client },
            origination,
            new FakeLoanRateService());

        var result = await controller.ConfirmAssignment(
            new LoanRiskWarningViewModel
            {
                ClientId = client.Id,
                CapitalAmount = 10_000m,
                TermInMonths = 12,
                AnnualInterestRate = 12m
            },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(LoansController.Details), redirect.ActionName);
        Assert.True(origination.ReceivedCreateRequest?.ConfirmHighRisk);
        Assert.Equal(1, origination.AssessCalls);
        Assert.Equal(1, origination.CreateCalls);
    }

    [Fact]
    public async Task Details_when_loan_does_not_exist_returns_not_found()
    {
        var controller = CreateController(
            new FakeLoanService(),
            new FakeClientSelectionService(),
            new FakeOriginationService(),
            new FakeLoanRateService());

        var result = await controller.Details(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditRate_get_maps_current_loan_presentation()
    {
        var detail = CreateDetail();
        var controller = CreateController(
            new FakeLoanService { Detail = detail },
            new FakeClientSelectionService(),
            new FakeOriginationService(),
            new FakeLoanRateService());

        var result = await controller.EditRate(
            detail.Id,
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EditLoanRateViewModel>(view.Model);
        Assert.Equal(detail.Id, model.LoanId);
        Assert.Equal(detail.LoanNumber, model.LoanNumber);
        Assert.Equal(detail.AnnualInterestRate, model.CurrentAnnualInterestRate);
        Assert.Equal(detail.AnnualInterestRate, model.AnnualInterestRate);
    }

    [Fact]
    public async Task EditRate_domain_failure_returns_view_with_spanish_error()
    {
        var detail = CreateDetail();
        var rate = new FakeLoanRateService
        {
            Result = OperationResult.Failure(
                LoanErrors.NoFuturePendingInstallments)
        };
        var controller = CreateController(
            new FakeLoanService { Detail = detail },
            new FakeClientSelectionService(),
            new FakeOriginationService(),
            rate);

        var result = await controller.EditRate(
            new EditLoanRateViewModel
            {
                LoanId = detail.Id,
                AnnualInterestRate = 14m
            },
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("cuotas futuras pendientes"));
        Assert.Equal(14m, rate.ReceivedRequest?.AnnualInterestRate);
    }

    [Fact]
    public async Task EditRate_email_failure_keeps_success_and_shows_warning()
    {
        var detail = CreateDetail();
        var rate = new FakeLoanRateService
        {
            Result = OperationResult.Success(),
            HasNotificationWarning = true
        };
        var controller = CreateController(
            new FakeLoanService { Detail = detail },
            new FakeClientSelectionService(),
            new FakeOriginationService(),
            rate);

        var result = await controller.EditRate(
            new EditLoanRateViewModel
            {
                LoanId = detail.Id,
                AnnualInterestRate = 14m
            },
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            "La tasa fue actualizada correctamente, pero no fue posible enviar el correo de notificación.",
            controller.TempData["SuccessMessage"]);
    }

    private static LoansController CreateController(
        FakeLoanService loans,
        FakeClientSelectionService selection,
        FakeOriginationService origination,
        FakeLoanRateService rate)
    {
        var httpContext = new DefaultHttpContext();

        return new LoansController(
            loans,
            selection,
            origination,
            rate)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new FakeTempDataProvider())
        };
    }

    private static PagedResult<LoanSummaryDto> EmptyLoanPage() =>
        new([], 1, 20, 0);

    private static LoanClientSelectionResult ClientPage()
    {
        var client = CreateClient();

        return new LoanClientSelectionResult(
            new PagedResult<LoanClientCandidateDto>(
                [client],
                1,
                20,
                1),
            225m);
    }

    private static LoanClientCandidateDto CreateClient() =>
        new(
            "client-1",
            "00100000001",
            "Ana Pérez",
            "ana@example.com",
            350m);

    private static HighRiskAssessmentDto NoRiskAssessment() =>
        new(
            LoanRiskType.None.ToString(),
            0m,
            10_661.88m,
            20_000m,
            false);

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
        public PagedResult<LoanSummaryDto> ListResult { get; init; } =
            EmptyLoanPage();
        public LoanDetailDto? Detail { get; init; }
        public LoanListRequest? ReceivedListRequest { get; private set; }

        public Task<PagedResult<LoanSummaryDto>> ListAsync(
            LoanListRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedListRequest = request;
            return Task.FromResult(ListResult);
        }

        public Task<LoanDetailDto?> GetDetailAsync(
            Guid loanId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<LoanDetailDto?> GetClientDetailAsync(
            Guid loanId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ClientLoanPortfolioItemDto?> GetClientActiveLoanAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeClientSelectionService
        : ILoanClientSelectionService
    {
        public LoanClientSelectionResult SearchResult { get; init; } =
            ClientPage();
        public LoanClientCandidateDto? Client { get; init; }
        public LoanClientSearchRequest? ReceivedSearchRequest { get; private set; }

        public Task<LoanClientSelectionResult> SearchAsync(
            LoanClientSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedSearchRequest = request;
            return Task.FromResult(SearchResult);
        }

        public Task<LoanClientCandidateDto?> GetEligibleClientAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Client);
    }

    private sealed class FakeOriginationService : ILoanOriginationService
    {
        public OperationResult<HighRiskAssessmentDto> AssessmentResult { get; init; } =
            OperationResult<HighRiskAssessmentDto>.Success(NoRiskAssessment());
        public OperationResult<LoanDetailDto> CreateResult { get; init; } =
            OperationResult<LoanDetailDto>.Failure(LoanErrors.ClientNotFound);
        public CreateLoanRequest? ReceivedCreateRequest { get; private set; }
        public int AssessCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public bool HasNotificationWarning { get; init; }

        public Task<OperationResult<HighRiskAssessmentDto>> AssessRiskAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default)
        {
            AssessCalls++;
            return Task.FromResult(AssessmentResult);
        }

        public Task<LoanOperationResult<LoanDetailDto>> CreateAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedCreateRequest = request;
            CreateCalls++;
            return Task.FromResult(
                new LoanOperationResult<LoanDetailDto>(
                    CreateResult,
                    HasNotificationWarning));
        }
    }

    private sealed class FakeLoanRateService : ILoanRateService
    {
        public OperationResult Result { get; init; } =
            OperationResult.Success();
        public UpdateLoanRateRequest? ReceivedRequest { get; private set; }
        public bool HasNotificationWarning { get; init; }

        public Task<LoanOperationResult> UpdateRateAsync(
            UpdateLoanRateRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            return Task.FromResult(
                new LoanOperationResult(
                    Result,
                    HasNotificationWarning));
        }
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(
            HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
