using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.CreditCards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClientCreditCardsController = ABP.WebApp.Areas.Client.Controllers.CreditCardsController;

namespace ABP.WebApp.IntegrationTests;

public sealed class ClientCreditCardsControllerTests
{
    [Fact]
    public void Controller_declares_client_area_and_role()
    {
        var type = typeof(ClientCreditCardsController);

        Assert.Equal("Client", type.GetCustomAttribute<AreaAttribute>()?.RouteValue);
        Assert.Equal(
            nameof(Roles.Client),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }

    [Fact]
    public async Task Details_returns_the_authenticated_clients_safe_card()
    {
        var detail = CreateDetail();
        var service = new FakeCreditCardService { ClientDetail = detail };
        var controller = new ClientCreditCardsController(service);

        var result = await controller.Details(
            detail.Id,
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreditCardDetailViewModel>(view.Model);
        Assert.Same(detail, model.Card);
        Assert.Equal(detail.Id, service.ReceivedClientCardId);
    }

    [Fact]
    public async Task Details_returns_not_found_when_card_is_missing_or_not_owned()
    {
        var controller = new ClientCreditCardsController(
            new FakeCreditCardService());

        var result = await controller.Details(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

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
        public CreditCardDetailDto? ClientDetail { get; init; }

        public Guid? ReceivedClientCardId { get; private set; }

        public Task<CreditCardDetailDto?> GetClientDetailAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default)
        {
            ReceivedClientCardId = creditCardId;
            return Task.FromResult(ClientDetail);
        }

        public Task<CreditCardListResult> ListAsync(
            CreditCardListRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCardDetailDto?> GetDetailAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OperationResult<Guid>> CreateAsync(
            CreateCreditCardRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OperationResult> UpdateLimitAsync(
            UpdateCreditLimitRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OperationResult> CancelAsync(
            CancelCreditCardRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
