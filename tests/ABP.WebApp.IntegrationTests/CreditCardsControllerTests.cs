using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Admin.Controllers;
using ABP.WebApp.Areas.Admin.ViewModels.CreditCards;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ABP.WebApp.IntegrationTests;

public sealed class CreditCardsControllerTests
{
    [Fact]
    public void Controller_declares_admin_area_role_and_antiforgery_on_every_post()
    {
        var type = typeof(CreditCardsController);

        Assert.Equal("Admin", type.GetCustomAttribute<AreaAttribute>()?.RouteValue);
        Assert.Equal(
            nameof(Roles.Administrator),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);

        var postActions = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<HttpPostAttribute>() is not null)
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
        var cards = new FakeCreditCardService
        {
            ListResult = EmptyCardList()
        };
        var controller = CreateController(cards, new FakeClientSelectionService());

        var result = await controller.Index(
            2,
            10,
            "00100000001",
            "cancelada",
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreditCardIndexViewModel>(view.Model);
        Assert.Same(cards.ListResult, model.Result);
        Assert.Equal(CreditCardStatusFilter.Cancelled, cards.ReceivedListRequest?.Status);
        Assert.Equal(2, cards.ReceivedListRequest?.Page);
    }

    [Fact]
    public async Task SelectClient_get_returns_active_clients_with_debt_summary()
    {
        var selection = new FakeClientSelectionService
        {
            SearchResult = ClientPage()
        };
        var controller = CreateController(new FakeCreditCardService(), selection);

        var result = await controller.SelectClient(
            1,
            20,
            " 001 ",
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreditCardClientSelectionViewModel>(view.Model);
        Assert.Same(selection.SearchResult, model.Result);
        Assert.Equal(" 001 ", selection.ReceivedSearchRequest?.Identification);
        Assert.Equal(350m, model.Result?.Page.Data.Single().TotalDebt);
        Assert.Equal(225m, model.Result?.AverageDebt);
    }

    [Fact]
    public async Task SelectClient_post_with_invalid_model_does_not_select_and_reloads_list()
    {
        var selection = new FakeClientSelectionService
        {
            SearchResult = ClientPage()
        };
        var controller = CreateController(new FakeCreditCardService(), selection);
        controller.ModelState.AddModelError("SelectedClientId", "Debe seleccionar un cliente.");
        var model = new CreditCardClientSelectionViewModel();

        var result = await controller.SelectClient(model, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, selection.GetClientCalls);
        Assert.Equal(1, selection.SearchCalls);
    }

    [Fact]
    public async Task SelectClient_post_with_active_client_redirects_to_create()
    {
        var client = CreateClient();
        var selection = new FakeClientSelectionService { Client = client };
        var controller = CreateController(new FakeCreditCardService(), selection);

        var result = await controller.SelectClient(
            new CreditCardClientSelectionViewModel { SelectedClientId = client.Id },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CreditCardsController.Create), redirect.ActionName);
        Assert.Equal(client.Id, redirect.RouteValues?["clientId"]);
    }

    [Fact]
    public async Task Create_post_converts_shared_validation_failure_to_model_state()
    {
        var client = CreateClient();
        var cards = new FakeCreditCardService
        {
            CreateException = new ValidationException(
                [new ValidationFailure("CreditLimit", "El límite debe ser mayor que cero.")])
        };
        var selection = new FakeClientSelectionService { Client = client };
        var controller = CreateController(cards, selection);

        var result = await controller.Create(
            new CreateCreditCardViewModel
            {
                ClientId = client.Id,
                CreditLimit = 0m
            },
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState[nameof(CreateCreditCardViewModel.CreditLimit)]!.Errors,
            error => error.ErrorMessage.Contains("mayor que cero"));
    }

    [Fact]
    public async Task Create_post_success_uses_prg_and_temp_data()
    {
        var cardId = Guid.NewGuid();
        var client = CreateClient();
        var cards = new FakeCreditCardService
        {
            CreateResult = OperationResult<Guid>.Success(cardId)
        };
        var controller = CreateController(
            cards,
            new FakeClientSelectionService { Client = client });

        var result = await controller.Create(
            new CreateCreditCardViewModel
            {
                ClientId = client.Id,
                CreditLimit = 2_000m
            },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CreditCardsController.Index), redirect.ActionName);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Create_post_notification_warning_preserves_successful_prg()
    {
        var client = CreateClient();
        var controller = CreateController(
            new FakeCreditCardService
            {
                CreateResult = OperationResult<Guid>.Success(Guid.NewGuid()),
                HasNotificationWarning = true
            },
            new FakeClientSelectionService { Client = client });

        var result = await controller.Create(
            new CreateCreditCardViewModel
            {
                ClientId = client.Id,
                CreditLimit = 2_000m
            },
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            "La tarjeta fue creada correctamente, pero no fue posible enviar el correo de notificación.",
            controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task EditLimit_notification_warning_preserves_confirmed_change()
    {
        var detail = CreateDetail();
        var controller = CreateController(
            new FakeCreditCardService
            {
                Detail = detail,
                UpdateResult = OperationResult.Success(),
                HasNotificationWarning = true
            },
            new FakeClientSelectionService());

        var result = await controller.EditLimit(
            new EditCreditLimitViewModel
            {
                CreditCardId = detail.Id,
                CreditLimit = 1_500m
            },
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            "El límite fue actualizado correctamente, pero no fue posible enviar el correo de notificación.",
            controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task EditLimit_domain_failure_returns_view_with_spanish_model_error()
    {
        var detail = CreateDetail();
        var cards = new FakeCreditCardService
        {
            Detail = detail,
            UpdateResult = OperationResult.Failure(CreditCardErrors.LimitBelowDebt)
        };
        var controller = CreateController(cards, new FakeClientSelectionService());

        var result = await controller.EditLimit(
            new EditCreditLimitViewModel
            {
                CreditCardId = detail.Id,
                CreditLimit = 100m
            },
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("inferior al monto adeudado"));
        Assert.Equal("************1234", cards.Detail?.MaskedCardNumber);
    }

    [Fact]
    public async Task Cancel_with_outstanding_debt_reloads_confirmation_using_only_last_four_digits()
    {
        var detail = CreateDetail();
        var cards = new FakeCreditCardService
        {
            Detail = detail,
            CancelResult = OperationResult.Failure(CreditCardErrors.OutstandingDebt)
        };
        var controller = CreateController(cards, new FakeClientSelectionService());
        var model = new CancelCreditCardViewModel { CreditCardId = detail.Id };

        var result = await controller.Cancel(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(CreditCardsController.ConfirmCancel), view.ViewName);
        Assert.Equal("1234", model.LastFourDigits);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("saldar la totalidad"));
    }

    [Fact]
    public async Task Details_when_card_does_not_exist_returns_not_found()
    {
        var controller = CreateController(
            new FakeCreditCardService(),
            new FakeClientSelectionService());

        var result = await controller.Details(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static CreditCardsController CreateController(
        FakeCreditCardService cards,
        FakeClientSelectionService selection)
    {
        var httpContext = new DefaultHttpContext();
        var controller = new CreditCardsController(cards, selection)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider())
        };

        return controller;
    }

    private static CreditCardListResult EmptyCardList() =>
        new(
            new PagedResult<CreditCardSummaryDto>(
                Array.Empty<CreditCardSummaryDto>(),
                1,
                20,
                0),
            CreditCardSearchStatus.NoSearch);

    private static CreditCardClientSelectionResult ClientPage()
    {
        var client = CreateClient();
        return new(
            new PagedResult<CreditCardClientCandidateDto>([client], 1, 20, 1),
            225m);
    }

    private static CreditCardClientCandidateDto CreateClient() =>
        new(
            "client-1",
            "00100000001",
            "Ana Pérez",
            "ana@example.com",
            350m);

    private static CreditCardDetailDto CreateDetail() =>
        new(
            Guid.NewGuid(),
            "************1234",
            "1234",
            "client-1",
            "Ana Pérez",
            1_000m,
            800m,
            200m,
            "08/29",
            "Activa",
            new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero),
            Array.Empty<CardConsumptionDto>());

    private sealed class FakeCreditCardService : ICreditCardService
    {
        public CreditCardListResult ListResult { get; init; } = EmptyCardList();

        public CreditCardDetailDto? Detail { get; init; }

        public OperationResult<Guid> CreateResult { get; init; } =
            OperationResult<Guid>.Failure(CreditCardErrors.ClientNotFound);

        public OperationResult UpdateResult { get; init; } = OperationResult.Success();

        public bool HasNotificationWarning { get; init; }

        public OperationResult CancelResult { get; init; } = OperationResult.Success();

        public ValidationException? CreateException { get; init; }

        public CreditCardListRequest? ReceivedListRequest { get; private set; }

        public Task<CreditCardListResult> ListAsync(
            CreditCardListRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedListRequest = request;
            return Task.FromResult(ListResult);
        }

        public Task<CreditCardDetailDto?> GetDetailAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<CreditCardDetailDto?> GetClientDetailAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<CardOperationResult<Guid>> CreateAsync(
            CreateCreditCardRequest request,
            CancellationToken cancellationToken = default) =>
            CreateException is null
                ? Task.FromResult(
                    new CardOperationResult<Guid>(
                        CreateResult,
                        HasNotificationWarning))
                : Task.FromException<CardOperationResult<Guid>>(CreateException);

        public Task<CardOperationResult> UpdateLimitAsync(
            UpdateCreditLimitRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new CardOperationResult(
                    UpdateResult,
                    HasNotificationWarning));

        public Task<OperationResult> CancelAsync(
            CancelCreditCardRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CancelResult);
    }

    private sealed class FakeClientSelectionService : ICreditCardClientSelectionService
    {
        public CreditCardClientSelectionResult SearchResult { get; init; } = ClientPage();

        public CreditCardClientCandidateDto? Client { get; init; }

        public CreditCardClientSearchRequest? ReceivedSearchRequest { get; private set; }

        public int SearchCalls { get; private set; }

        public int GetClientCalls { get; private set; }

        public Task<CreditCardClientSelectionResult> SearchAsync(
            CreditCardClientSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            ReceivedSearchRequest = request;
            return Task.FromResult(SearchResult);
        }

        public Task<CreditCardClientCandidateDto?> GetActiveClientAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            GetClientCalls++;
            return Task.FromResult(Client);
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
