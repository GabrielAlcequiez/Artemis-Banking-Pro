using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Queries.AssessLoanRisk;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Queries.AssessLoanRisk;

public sealed class AssessLoanRiskQueryTests
{
    [Fact]
    public async Task Validator_reuses_shared_create_loan_request_rules()
    {
        var validator = new AssessLoanRiskQueryValidator(
            new CreateLoanRequestValidator());
        var query = new AssessLoanRiskQuery(
            new CreateLoanRequest(
                string.Empty,
                0m,
                7,
                -1m));

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.ClientId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.CapitalAmount");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.TermInMonths");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.AnnualInterestRate");
    }

    [Fact]
    public async Task Handler_returns_complete_risk_assessment_from_service()
    {
        var request = CreateRequest();
        var assessment = new HighRiskAssessmentDto(
            "ProjectedHighRisk",
            500m,
            11_161.88m,
            1_000m,
            true);
        var service = new StubLoanOriginationService
        {
            AssessResult = OperationResult<HighRiskAssessmentDto>.Success(
                assessment)
        };
        var handler = new AssessLoanRiskQueryHandler(service);
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new AssessLoanRiskQuery(request),
            cancellationSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(assessment, result.Value);
        Assert.True(result.Value.RequiresConfirmation);
        Assert.Equal("ProjectedHighRisk", result.Value.RiskType);
        Assert.Equal(500m, result.Value.CurrentDebt);
        Assert.Equal(11_161.88m, result.Value.ProjectedDebt);
        Assert.Equal(1_000m, result.Value.AverageDebt);
        Assert.Same(request, service.ReceivedAssessRequest);
        Assert.Equal(
            cancellationSource.Token,
            service.ReceivedCancellationToken);
        Assert.Equal(1, service.AssessCalls);
    }

    [Fact]
    public async Task Handler_preserves_client_eligibility_failure()
    {
        var service = new StubLoanOriginationService
        {
            AssessResult = OperationResult<HighRiskAssessmentDto>.Failure(
                LoanErrors.ActiveLoanExists)
        };
        var handler = new AssessLoanRiskQueryHandler(service);

        var result = await handler.Handle(
            new AssessLoanRiskQuery(CreateRequest()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.ActiveLoanExists, result.Error);
        Assert.Equal(1, service.AssessCalls);
    }

    private static CreateLoanRequest CreateRequest() =>
        new(
            "client-1",
            10_000m,
            12,
            12m);

    private sealed class StubLoanOriginationService
        : ILoanOriginationService
    {
        public required OperationResult<HighRiskAssessmentDto> AssessResult { get; init; }
        public CreateLoanRequest? ReceivedAssessRequest { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }
        public int AssessCalls { get; private set; }

        public Task<OperationResult<HighRiskAssessmentDto>> AssessRiskAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedAssessRequest = request;
            ReceivedCancellationToken = cancellationToken;
            AssessCalls++;
            return Task.FromResult(AssessResult);
        }

        public Task<OperationResult<LoanDetailDto>> CreateAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
