using System.Reflection;
using System.Text.Json;
using ABP.Application.Common;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.Commands.CancelCreditCard;
using ABP.Application.Features.CreditCards.Commands.CreateCreditCard;
using ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Queries.GetCreditCardDetail;
using ABP.Application.Features.CreditCards.Queries.GetCreditCards;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApi.Controllers;
using ABP.WebApi.Models.CreditCards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.IntegrationTests;

public sealed class CreditCardsControllerTests
{
    [Fact]
    public void Controller_declares_exact_route_and_administrator_authorization()
    {
        var type = typeof(CreditCardsController);

        Assert.Equal(
            "api/v{version:apiVersion}/[controller]",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal(
            nameof(Roles.Administrator),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
        Assert.DoesNotContain(
            type.GetCustomAttributes(inherit: true),
            attribute => attribute.GetType().Name.Contains("ProducesResponseType"));
    }

    [Fact]
    public async Task GetAll_translates_spanish_status_and_returns_only_uniform_page()
    {
        var page = new PagedResult<CreditCardSummaryDto>(
            [CreateSummary()],
            1,
            20,
            1);
        var sender = new FakeSender();
        sender.Enqueue(new CreditCardListResult(page, CreditCardSearchStatus.ResultsFound));
        var controller = CreateController(sender);

        var result = await controller.GetAll(
            new CreditCardListApiRequest
            {
                Identification = "00100000001",
                Status = "activa"
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, ok.Value);
        var query = Assert.IsType<GetCreditCardsQuery>(Assert.Single(sender.Requests));
        Assert.Equal(CreditCardStatusFilter.Active, query.Request.Status);
        Assert.Equal("00100000001", query.Request.Identification);
    }

    [Fact]
    public async Task GetAll_with_unknown_status_returns_spanish_problem_without_dispatching()
    {
        var sender = new FakeSender();
        var controller = CreateController(sender);

        var result = await controller.GetAll(
            new CreditCardListApiRequest { Status = "bloqueada" },
            CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status400BadRequest);
        Assert.Contains("activa, cancelada o todas", problem.Detail);
        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task GetById_when_missing_returns_404_problem()
    {
        var sender = new FakeSender();
        sender.Enqueue<CreditCardDetailDto?>(null);
        var controller = CreateController(sender);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status404NotFound);
        Assert.Equal("La tarjeta seleccionada no existe.", problem.Detail);
        Assert.IsType<GetCreditCardDetailQuery>(Assert.Single(sender.Requests));
    }

    [Fact]
    public async Task GetById_returns_safe_detail_with_created_at_and_consumptions()
    {
        var cardId = Guid.NewGuid();
        var detail = CreateDetail(cardId) with
        {
            Consumptions =
            [
                new CardConsumptionDto(
                    Guid.NewGuid(),
                    new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero),
                    25m,
                    "AVANCE",
                    "APROBADO")
            ]
        };
        var sender = new FakeSender();
        sender.Enqueue<CreditCardDetailDto?>(detail);
        var controller = CreateController(sender);

        var result = await controller.GetById(cardId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CreditCardDetailDto>(ok.Value);
        Assert.Equal(detail.CreatedAt, response.CreatedAt);
        Assert.Equal("AVANCE", Assert.Single(response.Consumptions).CommerceName);
        AssertSafeJson(response);
    }

    [Fact]
    public async Task Create_returns_201_safe_representation_and_dispatches_command_then_detail()
    {
        var cardId = Guid.NewGuid();
        var sender = new FakeSender();
        sender.Enqueue(OperationResult<Guid>.Success(cardId));
        sender.Enqueue<CreditCardDetailDto?>(CreateDetail(cardId));
        var controller = CreateController(sender);

        var result = await controller.Create(
            new CreateCreditCardRequest("client-1", 1_000m),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CreditCardsController.GetById), created.ActionName);
        var response = Assert.IsType<CreditCardCreatedResponse>(created.Value);
        Assert.Equal(cardId, response.Id);
        Assert.Collection(
            sender.Requests,
            request => Assert.IsType<CreateCreditCardCommand>(request),
            request => Assert.IsType<GetCreditCardDetailQuery>(request));

        AssertSafeJson(response);
    }

    [Theory]
    [MemberData(nameof(CreateFailures))]
    public async Task Create_maps_expected_failure_to_problem(
        Error error,
        int expectedStatus)
    {
        var sender = new FakeSender();
        sender.Enqueue(OperationResult<Guid>.Failure(error));
        var controller = CreateController(sender);

        var result = await controller.Create(
            new CreateCreditCardRequest("client-1", 1_000m),
            CancellationToken.None);

        AssertProblem(result, expectedStatus);
        Assert.Single(sender.Requests);
    }

    [Fact]
    public async Task UpdateLimit_combines_route_id_with_body_and_returns_204()
    {
        var cardId = Guid.NewGuid();
        var sender = new FakeSender();
        sender.Enqueue(OperationResult.Success());
        var controller = CreateController(sender);

        var result = await controller.UpdateLimit(
            cardId,
            new UpdateCreditLimitApiRequest { CreditLimit = 2_500m },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<UpdateCreditLimitCommand>(Assert.Single(sender.Requests));
        Assert.Equal(cardId, command.Request.CreditCardId);
        Assert.Equal(2_500m, command.Request.CreditLimit);
    }

    [Fact]
    public async Task Cancel_when_card_is_missing_returns_404_problem()
    {
        var sender = new FakeSender();
        sender.Enqueue(OperationResult.Failure(CreditCardErrors.NotFound));
        var controller = CreateController(sender);

        var result = await controller.Cancel(Guid.NewGuid(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status404NotFound);
        Assert.IsType<CancelCreditCardCommand>(Assert.Single(sender.Requests));
    }

    public static TheoryData<Error, int> CreateFailures => new()
    {
        { CreditCardErrors.ClientNotFound, StatusCodes.Status404NotFound },
        { CreditCardErrors.ClientInactive, StatusCodes.Status400BadRequest },
        { CreditCardErrors.NumberGenerationFailed, StatusCodes.Status409Conflict }
    };

    private static CreditCardsController CreateController(FakeSender sender) =>
        new(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    private static ProblemDetails AssertProblem(ActionResult result, int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.DoesNotContain("errorCode", problem.Extensions.Keys);
        return problem;
    }

    private static void AssertSafeJson(object value)
    {
        var json = JsonSerializer.Serialize(value).ToLowerInvariant();
        Assert.DoesNotContain("cvc", json);
        Assert.DoesNotContain("hash", json);
        Assert.DoesNotContain("4000000000001234", json);
        Assert.DoesNotContain("0000000000001234", json);
    }

    private static CreditCardSummaryDto CreateSummary() =>
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
            new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));

    private static CreditCardDetailDto CreateDetail(Guid cardId) =>
        new(
            cardId,
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

    private sealed class FakeSender : ISender
    {
        private readonly Queue<object?> responses = new();

        public List<object> Requests { get; } = [];

        public void Enqueue<TResponse>(TResponse response) => responses.Enqueue(response);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult((TResponse)responses.Dequeue()!);
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(
            object request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(responses.Dequeue());
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
