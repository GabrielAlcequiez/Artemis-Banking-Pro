using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.Commerce;
using ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;
using ABP.Application.Features.Commerce.Commands.CreateCommerce;
using ABP.Application.Features.Commerce.Commands.UpdateCommerce;
using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Queries.GetCommerceDetail;
using ABP.Application.Features.Commerce.Queries.GetCommerces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApi.Controllers;
using ABP.WebApi.Models.Commerce;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.IntegrationTests;

public sealed class CommerceControllerTests
{
    [Fact]
    public void Controller_declares_exact_route_and_administrator_authorization()
    {
        var type = typeof(CommerceController);

        Assert.Equal(
            "api/v{version:apiVersion}/[controller]",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal(
            nameof(Roles.Administrator),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("activo", CommerceStatusFilter.Active)]
    [InlineData("inactivo", CommerceStatusFilter.Inactive)]
    [InlineData("todos", CommerceStatusFilter.All)]
    public async Task GetAll_translates_documented_status_values(
        string? apiStatus,
        CommerceStatusFilter? expectedStatus)
    {
        var page = new PagedResult<CommerceSummaryDto>(
            Array.Empty<CommerceSummaryDto>(),
            2,
            10,
            0);
        var sender = new FakeSender();
        sender.Enqueue(page);
        var controller = CreateController(sender);

        var result = await controller.GetAll(
            new CommerceListApiRequest
            {
                Page = 2,
                PageSize = 10,
                Status = apiStatus
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, ok.Value);
        var query = Assert.IsType<GetCommercesQuery>(
            Assert.Single(sender.Requests));
        Assert.Equal(2, query.Request.Page);
        Assert.Equal(10, query.Request.PageSize);
        Assert.Equal(expectedStatus, query.Request.Status);
    }

    [Fact]
    public async Task GetAll_rejects_unknown_status_without_dispatching()
    {
        var sender = new FakeSender();
        var controller = CreateController(sender);

        var result = await controller.GetAll(
            new CommerceListApiRequest { Status = "suspendido" },
            CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status400BadRequest);
        Assert.Contains("activo, inactivo o todos", problem.Detail);
        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task GetById_when_missing_returns_not_found()
    {
        var sender = new FakeSender();
        sender.Enqueue<CommerceDetailDto?>(null);
        var controller = CreateController(sender);

        var result = await controller.GetById(
            Guid.NewGuid(),
            CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status404NotFound);
        Assert.Equal(CommerceErrors.NotFound.Description, problem.Detail);
    }

    [Fact]
    public async Task Create_returns_documented_safe_representation()
    {
        var commerceId = Guid.NewGuid();
        var detail = CreateDetail(commerceId);
        var sender = new FakeSender();
        sender.Enqueue(OperationResult<Guid>.Success(commerceId));
        sender.Enqueue<CommerceDetailDto?>(detail);
        var controller = CreateController(sender);

        var result = await controller.Create(
            CreateRequest(),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CommerceController.GetById), created.ActionName);
        Assert.Equal(commerceId, created.RouteValues?["id"]);
        var response = Assert.IsType<CommerceCreatedResponse>(created.Value);
        Assert.Equal(commerceId, response.Id);
        Assert.True(response.IsActive);
        Assert.Collection(
            sender.Requests,
            request => Assert.IsType<CreateCommerceCommand>(request),
            request => Assert.IsType<GetCommerceDetailQuery>(request));
    }

    [Theory]
    [MemberData(nameof(WriteFailures))]
    public async Task Create_maps_expected_failure_to_problem(
        Error error,
        int expectedStatus)
    {
        var sender = new FakeSender();
        sender.Enqueue(OperationResult<Guid>.Failure(error));
        var controller = CreateController(sender);

        var result = await controller.Create(
            CreateRequest(),
            CancellationToken.None);

        AssertProblem(result, expectedStatus);
        Assert.Single(sender.Requests);
    }

    [Fact]
    public async Task Update_uses_route_id_and_returns_no_content()
    {
        var commerceId = Guid.NewGuid();
        var sender = new FakeSender();
        sender.Enqueue(OperationResult.Success());
        var controller = CreateController(sender);

        var result = await controller.Update(
            commerceId,
            new UpdateCommerceApiRequest
            {
                Name = "Tienda Actualizada",
                Description = "Nueva descripción",
                Email = "nuevo@tienda.com",
                PhoneNumber = "8095559876",
                Rnc = "101888888"
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<UpdateCommerceCommand>(
            Assert.Single(sender.Requests));
        Assert.Equal(commerceId, command.Request.CommerceId);
        Assert.Equal("Tienda Actualizada", command.Request.Name);
    }

    [Fact]
    public async Task ChangeStatus_rejects_missing_status_without_dispatching()
    {
        var sender = new FakeSender();
        var controller = CreateController(sender);

        var result = await controller.ChangeStatus(
            Guid.NewGuid(),
            new ChangeCommerceStatusApiRequest(),
            CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status400BadRequest);
        Assert.Equal("El campo status es requerido.", problem.Detail);
        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task ChangeStatus_preserves_false_and_returns_no_content()
    {
        var commerceId = Guid.NewGuid();
        var sender = new FakeSender();
        sender.Enqueue(OperationResult.Success());
        var controller = CreateController(sender);

        var result = await controller.ChangeStatus(
            commerceId,
            new ChangeCommerceStatusApiRequest { Status = false },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<ChangeCommerceStatusCommand>(
            Assert.Single(sender.Requests));
        Assert.Equal(commerceId, command.Request.CommerceId);
        Assert.False(command.Request.IsActive);
    }

    public static TheoryData<Error, int> WriteFailures => new()
    {
        { CommerceErrors.NotFound, StatusCodes.Status404NotFound },
        { CommerceErrors.DuplicateEmail, StatusCodes.Status409Conflict },
        { CommerceErrors.DuplicateRnc, StatusCodes.Status409Conflict },
        { CommerceErrors.AdministratorRequired, StatusCodes.Status403Forbidden }
    };

    private static CommerceController CreateController(FakeSender sender) =>
        new(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    private static CreateCommerceRequest CreateRequest() =>
        new(
            "Tienda Demo",
            "Comercio de prueba",
            "contacto@tiendademo.com",
            "8095551234",
            "101999999");

    private static CommerceDetailDto CreateDetail(Guid commerceId) =>
        new(
            commerceId,
            "Tienda Demo",
            "Comercio de prueba",
            "contacto@tiendademo.com",
            "8095551234",
            "101999999",
            true,
            new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero),
            null);

    private static ProblemDetails AssertProblem(
        ActionResult result,
        int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.DoesNotContain("errorCode", problem.Extensions.Keys);
        return problem;
    }

    private sealed class FakeSender : ISender
    {
        private readonly Queue<object?> responses = new();

        public List<object> Requests { get; } = [];

        public void Enqueue<TResponse>(TResponse response) =>
            responses.Enqueue(response);

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
