using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Cashier.ViewModels.CreditCards;
using ABP.WebApp.Areas.Client.ViewModels.CreditCards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CashierPaymentsController = ABP.WebApp.Areas.Cashier.Controllers.CreditCardPaymentsController;
using ClientCashAdvancesController = ABP.WebApp.Areas.Client.Controllers.CashAdvancesController;
using ClientPaymentsController = ABP.WebApp.Areas.Client.Controllers.CreditCardPaymentsController;

namespace ABP.WebApp.IntegrationTests;

public sealed class CardOperationsControllerTests
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
    public async Task Client_payment_success_maps_request_and_uses_prg()
    {
        var service = new FakeCardPaymentService
        {
            ProcessResult = OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    Guid.NewGuid(),
                    100m,
                    DateTimeOffset.UtcNow))
        };
        var controller = Configure(new ClientPaymentsController(service));
        var model = new CreditCardPaymentViewModel
        {
            CreditCardId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            Amount = 100m,
            OperationId = Guid.NewGuid()
        };

        var result = await controller.Create(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Client", redirect.RouteValues?["area"]);
        Assert.Equal(model.OperationId, service.ReceivedPayment?.OperationId);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Client_payment_failure_reloads_options_and_model_error()
    {
        var service = new FakeCardPaymentService
        {
            Options = CreateOptions(),
            ProcessResult = OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.InsufficientFunds)
        };
        var controller = Configure(new ClientPaymentsController(service));
        var model = new CreditCardPaymentViewModel
        {
            CreditCardId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            Amount = 500m,
            OperationId = Guid.NewGuid()
        };

        var result = await controller.Create(model, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(1, service.OptionsCalls);
        Assert.NotEmpty(model.CreditCards);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("monto requerido"));
    }

    [Fact]
    public async Task Cashier_confirm_uses_safe_preview_without_full_pan()
    {
        var preview = new CashierCardPaymentPreview(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Luis Díaz",
            "123456789",
            "Ana Pérez",
            "1234",
            1_000m,
            500m);
        var service = new FakeCardPaymentService
        {
            PreviewResult = OperationResult<CashierCardPaymentPreview>.Success(preview)
        };
        var controller = Configure(new CashierPaymentsController(service));

        var result = await controller.Confirm(
            new CashierCreditCardPaymentViewModel
            {
                SourceAccountNumber = "123456789",
                CreditCardNumber = "4000000000001234",
                Amount = 1_000m,
                OperationId = preview.OperationId
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CashierCreditCardPaymentConfirmationViewModel>(
            view.Model);
        Assert.Equal("1234", model.CardLastFourDigits);
        Assert.Equal(500m, model.EffectiveAmount);
        Assert.DoesNotContain(
            model.GetType().GetProperties(),
            property => property.Name == "CreditCardNumber");
    }

    [Fact]
    public async Task Cashier_confirm_failure_removes_full_pan_from_model_state_and_view_model()
    {
        const string fullPan = "4000000000001234";
        var service = new FakeCardPaymentService
        {
            PreviewResult = OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.CardNotFound)
        };
        var controller = Configure(new CashierPaymentsController(service));
        var input = new CashierCreditCardPaymentViewModel
        {
            SourceAccountNumber = "123456789",
            CreditCardNumber = fullPan,
            Amount = 100m,
            OperationId = Guid.NewGuid()
        };
        controller.ModelState.SetModelValue(
            nameof(input.CreditCardNumber),
            fullPan,
            fullPan);

        var result = await controller.Confirm(input, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CashierCreditCardPaymentViewModel>(view.Model);
        Assert.Equal(string.Empty, model.CreditCardNumber);
        Assert.False(
            controller.ModelState.ContainsKey(nameof(input.CreditCardNumber)));
        Assert.DoesNotContain(
            fullPan,
            string.Join(
                ' ',
                controller.ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)));
    }

    [Fact]
    public async Task Cashier_execute_success_uses_prg()
    {
        var service = new FakeCardPaymentService
        {
            ProcessResult = OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    Guid.NewGuid(),
                    250m,
                    DateTimeOffset.UtcNow))
        };
        var controller = Configure(new CashierPaymentsController(service));

        var result = await controller.Execute(
            new CashierCreditCardPaymentConfirmationViewModel
            {
                CreditCardId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                RequestedAmount = 250m,
                OperationId = Guid.NewGuid()
            },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Cashier", redirect.RouteValues?["area"]);
    }

    [Fact]
    public async Task Cash_advance_success_maps_request_and_uses_prg()
    {
        var service = new FakeCashAdvanceService
        {
            ProcessResult = OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    Guid.NewGuid(),
                    100m,
                    DateTimeOffset.UtcNow))
        };
        var controller = Configure(new ClientCashAdvancesController(service));
        var model = new CashAdvanceViewModel
        {
            CreditCardId = Guid.NewGuid(),
            TargetAccountId = Guid.NewGuid(),
            Amount = 100m,
            OperationId = Guid.NewGuid()
        };

        var result = await controller.Execute(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Client", redirect.RouteValues?["area"]);
        Assert.Equal(model.OperationId, service.ReceivedRequest?.OperationId);
    }

    public static TheoryData<Type, string, string> ControllerCases => new()
    {
        { typeof(ClientPaymentsController), "Client", nameof(Roles.Client) },
        { typeof(ClientCashAdvancesController), "Client", nameof(Roles.Client) },
        { typeof(CashierPaymentsController), "Cashier", nameof(Roles.Cashier) }
    };

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

    private static ClientCardOperationOptions CreateOptions() =>
        new(
            [new CreditCardOperationOptionDto(
                Guid.NewGuid(),
                "************1234",
                500m,
                500m,
                "08/29")],
            [new SavingsAccountOperationOptionDto(
                Guid.NewGuid(),
                "123456789",
                1_000m)]);

    private sealed class FakeCardPaymentService : ICardPaymentService
    {
        public ClientCardOperationOptions Options { get; init; } = CreateOptions();
        public int OptionsCalls { get; private set; }
        public CreditCardPaymentRequest? ReceivedPayment { get; private set; }
        public OperationResult<CashierCardPaymentPreview> PreviewResult { get; init; } =
            OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.CardNotFound);
        public OperationResult<FinancialOperationReceipt> ProcessResult { get; init; } =
            OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.CardNotFound);

        public Task<ClientCardOperationOptions> GetClientOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            OptionsCalls++;
            return Task.FromResult(Options);
        }

        public Task<OperationResult<CashierCardPaymentPreview>> PrepareCashierPaymentAsync(
            string sourceAccountNumber,
            string creditCardNumber,
            decimal amount,
            Guid operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PreviewResult);

        public Task<OperationResult<FinancialOperationReceipt>> ProcessPaymentAsync(
            CreditCardPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedPayment = request;
            return Task.FromResult(ProcessResult);
        }
    }

    private sealed class FakeCashAdvanceService : ICashAdvanceService
    {
        public CashAdvanceRequest? ReceivedRequest { get; private set; }
        public OperationResult<FinancialOperationReceipt> ProcessResult { get; init; } =
            OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.CardNotFound);

        public Task<ClientCardOperationOptions> GetClientOptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateOptions());

        public Task<OperationResult<FinancialOperationReceipt>> ProcessCashAdvanceAsync(
            CashAdvanceRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
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
