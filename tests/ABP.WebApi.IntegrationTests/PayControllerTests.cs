using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.HermesPay;
using ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;
using ABP.Domain.Enums;
using ABP.WebApi.Controllers;
using ABP.WebApi.Models.HermesPay;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.IntegrationTests;

public sealed class PayControllerTests
{
    [Fact]
    public void Controller_declares_documented_route_and_roles()
    {
        var type = typeof(PayController);

        Assert.Equal(
            "api/v{version:apiVersion}/[controller]",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal(typeof(BaseApiController), type.BaseType);
        Assert.Equal(
            "Administrator,Commerce",
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal(
            "get-transactions/{commerceId:guid}",
            type.GetMethod(nameof(PayController.GetTransactions))?
                .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal(
            "process-payment/{commerceId:guid}",
            type.GetMethod(nameof(PayController.ProcessPayment))?
                .GetCustomAttribute<HttpPostAttribute>()?.Template);

        Assert.Equal(
            [200, 400, 401, 403, 404],
            DeclaredStatuses(type.GetMethod(nameof(PayController.GetTransactions))!));
        Assert.Equal(
            [204, 400, 401, 403, 404, 409],
            DeclaredStatuses(type.GetMethod(nameof(PayController.ProcessPayment))!));
    }

    [Fact]
    public async Task GetTransactions_dispatches_query_and_returns_page()
    {
        var commerceId = Guid.NewGuid();
        var page = new HermesTransactionsPageDto(
            2,
            10,
            11,
            2,
            commerceId,
            "Tienda Hermes",
            []);
        var sender = new FakeSender(
            OperationResult<HermesTransactionsPageDto>.Success(page));
        var controller = CreateController(sender);

        var response = await controller.GetTransactions(
            commerceId,
            2,
            10,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        Assert.Same(page, ok.Value);
        var query = Assert.IsType<GetHermesTransactionsQuery>(sender.Request);
        Assert.Equal(commerceId, query.RequestedCommerceId);
        Assert.Equal(2, query.Page);
        Assert.Equal(10, query.PageSize);
    }

    [Theory]
    [MemberData(nameof(ExpectedProblems))]
    public async Task GetTransactions_maps_known_errors_to_problem_details(
        Error error,
        int expectedStatus)
    {
        var sender = new FakeSender(
            OperationResult<HermesTransactionsPageDto>.Failure(error));
        var controller = CreateController(sender);

        var response = await controller.GetTransactions(
            Guid.NewGuid(),
            1,
            20,
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(error.Description, problem.Detail);
        Assert.DoesNotContain("errorCode", problem.Extensions.Keys);
    }

    [Fact]
    public async Task ProcessPayment_maps_api_contract_and_returns_no_content()
    {
        var commerceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var sender = new FakeSender(
            OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    operationId,
                    689.25m,
                    DateTimeOffset.UtcNow)));
        var controller = CreateController(sender);

        var response = await controller.ProcessPayment(
            commerceId,
            operationId,
            new ProcessHermesPaymentApiRequest
            {
                CardNumber = "1589963258467598",
                MonthExpirationCard = "08",
                YearExpirationCard = "2029",
                Cvc = "123",
                TransactionAmount = 689.25m
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        var command = Assert.IsType<ProcessHermesPaymentCommand>(sender.Request);
        Assert.Equal(commerceId, command.Request.RequestedCommerceId);
        Assert.Equal(operationId, command.Request.OperationId);
        Assert.Equal(8, command.Request.ExpirationMonth);
        Assert.Equal(2029, command.Request.ExpirationYear);
        Assert.Equal(689.25m, command.Request.TransactionAmount);
    }

    [Theory]
    [InlineData("8", "2029")]
    [InlineData("13", "2029")]
    [InlineData("08", "29")]
    [InlineData("AA", "2029")]
    public async Task ProcessPayment_rejects_invalid_expiration_without_dispatching(
        string month,
        string year)
    {
        var sender = new FakeSender();
        var controller = CreateController(sender);

        var response = await controller.ProcessPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ProcessHermesPaymentApiRequest
            {
                CardNumber = "1589963258467598",
                MonthExpirationCard = month,
                YearExpirationCard = year,
                Cvc = "123",
                TransactionAmount = 100m
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Null(sender.Request);
    }

    [Fact]
    public async Task ProcessPayment_maps_reused_operation_with_different_data_to_conflict()
    {
        var sender = new FakeSender(
            OperationResult<FinancialOperationReceipt>.Failure(
                HermesPayErrors.OperationIdConflict));
        var controller = CreateController(sender);

        var response = await controller.ProcessPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ProcessHermesPaymentApiRequest
            {
                CardNumber = "1589963258467598",
                MonthExpirationCard = "08",
                YearExpirationCard = "2029",
                Cvc = "123",
                TransactionAmount = 100m
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(HermesPayErrors.OperationIdConflict.Description, problem.Detail);
    }

    public static TheoryData<Error, int> ExpectedProblems => new()
    {
        { HermesPayErrors.CommerceUserInactive, StatusCodes.Status403Forbidden },
        { HermesPayErrors.CommerceAssociationRequired, StatusCodes.Status403Forbidden },
        { HermesPayErrors.CommerceNotFound, StatusCodes.Status404NotFound },
        { HermesPayErrors.CommerceInactive, StatusCodes.Status400BadRequest },
        { HermesPayErrors.AssociatedCommerceUserInactive, StatusCodes.Status400BadRequest },
        { HermesPayErrors.PrimaryAccountRequired, StatusCodes.Status400BadRequest },
        { HermesPayErrors.InsufficientCredit, StatusCodes.Status400BadRequest }
    };

    private static PayController CreateController(FakeSender sender) =>
        new(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    private static int[] DeclaredStatuses(MethodInfo method) =>
        method.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(attribute => attribute.StatusCode)
            .ToArray();

    private sealed class FakeSender : ISender
    {
        private readonly Queue<object> responses = [];

        public FakeSender(params object[] configuredResponses)
        {
            foreach (var response in configuredResponses)
            {
                responses.Enqueue(response);
            }
        }

        public object? Request { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult((TResponse)responses.Dequeue());
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
