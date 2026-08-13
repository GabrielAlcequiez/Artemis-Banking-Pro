using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Cashier.ViewModels.Loans;
using ABP.WebApp.Areas.Client.ViewModels.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CashierLoanPaymentsController = ABP.WebApp.Areas.Cashier.Controllers.LoanPaymentsController;
using ClientLoanPaymentsController = ABP.WebApp.Areas.Client.Controllers.LoanPaymentsController;

namespace ABP.WebApp.IntegrationTests;

public sealed class LoanPaymentControllerTests
{
    [Theory]
    [MemberData(nameof(ControllerCases))]
    public void Controllers_declare_expected_area_role_and_antiforgery(
        Type controllerType,
        string expectedArea,
        string expectedRole)
    {
        Assert.Equal(
            expectedArea,
            controllerType.GetCustomAttribute<AreaAttribute>()?.RouteValue);
        Assert.Equal(
            expectedRole,
            controllerType.GetCustomAttribute<AuthorizeAttribute>()?.Roles);

        var postActions = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<HttpPostAttribute>() is not null)
            .ToArray();

        Assert.NotEmpty(postActions);
        Assert.All(
            postActions,
            method => Assert.NotNull(
                method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
    }

    [Fact]
    public async Task Client_create_get_loads_options_and_selects_the_active_loan()
    {
        var service = new FakeLoanPaymentService { Options = CreateOptions() };
        var controller = Configure(new ClientLoanPaymentsController(service));

        var result = await controller.Create(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoanPaymentViewModel>(view.Model);
        Assert.Equal(service.Options.Loans.Single().Id, model.LoanId);
        Assert.NotEqual(Guid.Empty, model.OperationId);
        Assert.Single(model.SavingsAccounts);
    }

    [Fact]
    public async Task Client_payment_success_maps_request_and_uses_prg()
    {
        var service = new FakeLoanPaymentService
        {
            ProcessResult = SuccessResult()
        };
        var controller = Configure(new ClientLoanPaymentsController(service));
        var model = new LoanPaymentViewModel
        {
            LoanId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            Amount = 100m,
            OperationId = Guid.NewGuid()
        };

        var result = await controller.Create(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Client", redirect.RouteValues?["area"]);
        Assert.Equal(model.OperationId, service.ReceivedPayment?.OperationId);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Client_payment_failure_reloads_options_and_error()
    {
        var service = new FakeLoanPaymentService
        {
            Options = CreateOptions(),
            ProcessResult = OperationResult<LoanPaymentResult>.Failure(
                LoanErrors.InsufficientFunds)
        };
        var controller = Configure(new ClientLoanPaymentsController(service));
        var model = new LoanPaymentViewModel
        {
            LoanId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            Amount = 500m,
            OperationId = Guid.NewGuid()
        };

        var result = await controller.Create(model, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(1, service.OptionsCalls);
        Assert.NotEmpty(model.Loans);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("fondos suficientes"));
    }

    [Fact]
    public async Task Cashier_confirm_maps_preview_with_different_owners()
    {
        var preview = new CashierLoanPaymentPreview(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Luis Díaz",
            "123456789",
            "Ana Pérez",
            "987654321",
            1_000m,
            500m);
        var service = new FakeLoanPaymentService
        {
            PreviewResult = OperationResult<CashierLoanPaymentPreview>.Success(preview)
        };
        var controller = Configure(new CashierLoanPaymentsController(service));

        var result = await controller.Confirm(
            new CashierLoanPaymentViewModel
            {
                SourceAccountNumber = "123456789",
                LoanNumber = "987654321",
                Amount = 1_000m,
                OperationId = preview.OperationId
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CashierLoanPaymentConfirmationViewModel>(
            view.Model);
        Assert.Equal("Luis Díaz", model.AccountOwnerFullName);
        Assert.Equal("Ana Pérez", model.LoanOwnerFullName);
        Assert.Equal(500m, model.EffectiveAmount);
    }

    [Fact]
    public async Task Cashier_execute_success_uses_prg()
    {
        var service = new FakeLoanPaymentService
        {
            ProcessResult = SuccessResult()
        };
        var controller = Configure(new CashierLoanPaymentsController(service));

        var result = await controller.Execute(
            new CashierLoanPaymentConfirmationViewModel
            {
                LoanId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                RequestedAmount = 250m,
                OperationId = Guid.NewGuid()
            },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Cashier", redirect.RouteValues?["area"]);
    }

    public static TheoryData<Type, string, string> ControllerCases => new()
    {
        { typeof(ClientLoanPaymentsController), "Client", nameof(Roles.Client) },
        { typeof(CashierLoanPaymentsController), "Cashier", nameof(Roles.Cashier) }
    };

    private static ClientLoanPaymentOptions CreateOptions() =>
        new(
            [new LoanOperationOptionDto(Guid.NewGuid(), "123456789", 500m)],
            [new SavingsAccountOperationOptionDto(
                Guid.NewGuid(),
                "987654321",
                1_000m)]);

    private static OperationResult<LoanPaymentResult> SuccessResult() =>
        OperationResult<LoanPaymentResult>.Success(
            new LoanPaymentResult(
                Guid.NewGuid(),
                "123456789",
                Guid.NewGuid(),
                100m,
                100m,
                400m,
                false,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));

    private static TController Configure<TController>(TController controller)
        where TController : Controller
    {
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(
            httpContext,
            new FakeTempDataProvider());
        return controller;
    }

    private sealed class FakeLoanPaymentService : ILoanPaymentService
    {
        public ClientLoanPaymentOptions Options { get; init; } = CreateOptions();
        public int OptionsCalls { get; private set; }
        public LoanPaymentRequest? ReceivedPayment { get; private set; }
        public OperationResult<CashierLoanPaymentPreview> PreviewResult { get; init; } =
            OperationResult<CashierLoanPaymentPreview>.Failure(LoanErrors.NotFound);
        public OperationResult<LoanPaymentResult> ProcessResult { get; init; } =
            OperationResult<LoanPaymentResult>.Failure(LoanErrors.NotFound);

        public Task<ClientLoanPaymentOptions> GetClientOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            OptionsCalls++;
            return Task.FromResult(Options);
        }

        public Task<OperationResult<CashierLoanPaymentPreview>> PrepareCashierPaymentAsync(
            string sourceAccountNumber,
            string loanNumber,
            decimal amount,
            Guid operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PreviewResult);

        public Task<OperationResult<LoanPaymentResult>> ProcessPaymentAsync(
            LoanPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedPayment = request;
            return Task.FromResult(ProcessResult);
        }
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
