using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.Commands.CreateLoan;
using ABP.Application.Features.Loans.Commands.UpdateLoanRate;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Queries.AssessLoanRisk;
using ABP.Application.Features.Loans.Queries.GetLoanDetail;
using ABP.Application.Features.Loans.Queries.GetLoans;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApi.Controllers;
using ABP.WebApi.Models.Loans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.IntegrationTests;

public sealed class LoansControllerTests
{
    [Fact]
    public void Controller_declares_exact_route_and_administrator_authorization()
    {
        var type = typeof(LoansController);

        Assert.Equal(
            "api/loan",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal(
            nameof(Roles.Administrator),
            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
    }

    [Fact]
    public async Task GetAll_translates_spanish_status_and_returns_uniform_page()
    {
        var page = new PagedResult<LoanSummaryDto>(
            [CreateSummary()],
            2,
            5,
            7);
        var sender = new FakeSender();
        sender.Enqueue(page);
        var controller = CreateController(sender);

        var result = await controller.GetAll(
            new LoanListApiRequest
            {
                Page = 2,
                PageSize = 5,
                Identification = "00100000001",
                Status = "completado"
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, ok.Value);
        var query = Assert.IsType<GetLoansQuery>(
            Assert.Single(sender.Requests));
        Assert.Equal(2, query.Request.Page);
        Assert.Equal(5, query.Request.PageSize);
        Assert.Equal("00100000001", query.Request.Identification);
        Assert.Equal(LoanStatusFilter.Completed, query.Request.Status);
    }

    [Fact]
    public async Task GetAll_with_unknown_status_returns_spanish_problem_without_dispatching()
    {
        var sender = new FakeSender();
        var controller = CreateController(sender);

        var result = await controller.GetAll(
            new LoanListApiRequest { Status = "cancelado" },
            CancellationToken.None);

        var problem = AssertProblem(
            result,
            StatusCodes.Status400BadRequest);
        Assert.Contains("activo, completado o todos", problem.Detail);
        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task GetById_when_missing_returns_404_problem()
    {
        var sender = new FakeSender();
        sender.Enqueue<LoanDetailDto?>(null);
        var controller = CreateController(sender);

        var result = await controller.GetById(
            Guid.NewGuid(),
            CancellationToken.None);

        var problem = AssertProblem(
            result,
            StatusCodes.Status404NotFound);
        Assert.Equal("El préstamo seleccionado no existe.", problem.Detail);
        Assert.IsType<GetLoanDetailQuery>(Assert.Single(sender.Requests));
    }

    [Fact]
    public async Task GetById_returns_complete_loan_detail()
    {
        var loanId = Guid.NewGuid();
        var detail = CreateDetail(loanId);
        var sender = new FakeSender();
        sender.Enqueue<LoanDetailDto?>(detail);
        var controller = CreateController(sender);

        var result = await controller.GetById(
            loanId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(detail, ok.Value);
        Assert.Equal(
            loanId,
            Assert.IsType<GetLoanDetailQuery>(
                Assert.Single(sender.Requests)).LoanId);
    }

    [Fact]
    public async Task Create_when_risk_requires_confirmation_returns_409_with_metrics()
    {
        var assessment = new HighRiskAssessmentDto(
            "ProjectedHighRisk",
            500m,
            11_161.88m,
            1_000m,
            true);
        var sender = new FakeSender();
        sender.Enqueue(
            OperationResult<HighRiskAssessmentDto>.Success(assessment));
        var controller = CreateController(sender);

        var result = await controller.Create(
            CreateRequest(),
            CancellationToken.None);

        var problem = AssertProblem(
            result,
            StatusCodes.Status409Conflict);
        Assert.Equal(
            "ProjectedHighRisk",
            problem.Extensions["riskType"]);
        Assert.Equal(500m, problem.Extensions["currentDebt"]);
        Assert.Equal(11_161.88m, problem.Extensions["projectedDebt"]);
        Assert.Equal(1_000m, problem.Extensions["averageDebt"]);
        Assert.IsType<AssessLoanRiskQuery>(
            Assert.Single(sender.Requests));
    }

    [Fact]
    public async Task Create_when_confirmed_returns_201_and_dispatches_risk_then_command()
    {
        var request = CreateRequest() with { ConfirmHighRisk = true };
        var detail = CreateDetail(Guid.NewGuid());
        var sender = new FakeSender();
        sender.Enqueue(
            OperationResult<HighRiskAssessmentDto>.Success(
                new HighRiskAssessmentDto(
                    "ProjectedHighRisk",
                    500m,
                    11_161.88m,
                    1_000m,
                    false)));
        sender.Enqueue(
            OperationResult<LoanDetailDto>.Success(detail));
        var controller = CreateController(sender);

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(LoansController.GetById), created.ActionName);
        Assert.Same(detail, created.Value);
        Assert.Equal(detail.Id, created.RouteValues?["id"]);
        Assert.Collection(
            sender.Requests,
            sent => Assert.Same(
                request,
                Assert.IsType<AssessLoanRiskQuery>(sent).Request),
            sent => Assert.Same(
                request,
                Assert.IsType<CreateLoanCommand>(sent).Request));
    }

    [Theory]
    [MemberData(nameof(CreateFailures))]
    public async Task Create_maps_expected_failure_to_problem(
        Error error,
        int expectedStatus)
    {
        var sender = new FakeSender();
        sender.Enqueue(
            OperationResult<HighRiskAssessmentDto>.Failure(error));
        var controller = CreateController(sender);

        var result = await controller.Create(
            CreateRequest(),
            CancellationToken.None);

        AssertProblem(result, expectedStatus);
        Assert.Single(sender.Requests);
    }

    [Fact]
    public async Task UpdateRate_combines_route_id_with_body_and_returns_204()
    {
        var loanId = Guid.NewGuid();
        var sender = new FakeSender();
        sender.Enqueue(OperationResult.Success());
        var controller = CreateController(sender);

        var result = await controller.UpdateRate(
            loanId,
            new UpdateLoanRateApiRequest
            {
                AnnualInterestRate = 14.5m
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<UpdateLoanRateCommand>(
            Assert.Single(sender.Requests));
        Assert.Equal(loanId, command.Request.LoanId);
        Assert.Equal(14.5m, command.Request.AnnualInterestRate);
    }

    [Fact]
    public async Task UpdateRate_when_loan_is_missing_returns_404_problem()
    {
        var sender = new FakeSender();
        sender.Enqueue(OperationResult.Failure(LoanErrors.NotFound));
        var controller = CreateController(sender);

        var result = await controller.UpdateRate(
            Guid.NewGuid(),
            new UpdateLoanRateApiRequest
            {
                AnnualInterestRate = 12m
            },
            CancellationToken.None);

        AssertProblem(result, StatusCodes.Status404NotFound);
    }

    public static TheoryData<Error, int> CreateFailures => new()
    {
        { LoanErrors.ClientNotFound, StatusCodes.Status404NotFound },
        { LoanErrors.ClientInactive, StatusCodes.Status400BadRequest },
        { LoanErrors.ActiveLoanExists, StatusCodes.Status409Conflict },
        { LoanErrors.PrincipalAccountNotFound, StatusCodes.Status409Conflict },
        { LoanErrors.NumberGenerationFailed, StatusCodes.Status409Conflict }
    };

    private static LoansController CreateController(FakeSender sender) =>
        new(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    private static ProblemDetails AssertProblem(
        ActionResult result,
        int expectedStatus)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        return problem;
    }

    private static CreateLoanRequest CreateRequest() =>
        new(
            "client-1",
            10_000m,
            12,
            12m);

    private static LoanSummaryDto CreateSummary() =>
        new(
            Guid.NewGuid(),
            "123456789",
            "client-1",
            "Ana Pérez",
            10_000m,
            12,
            0,
            10_661.88m,
            12m,
            12,
            "Activo",
            "Al día",
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    private static LoanDetailDto CreateDetail(Guid loanId) =>
        new(
            loanId,
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
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero),
            []);

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
