using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.Commands.CreateLoan;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Commands.CreateLoan;

public sealed class CreateLoanCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_create_loan_request_rules()
    {
        var validator = new CreateLoanCommandValidator(
            new CreateLoanRequestValidator());
        var command = new CreateLoanCommand(
            new CreateLoanRequest(
                string.Empty,
                0m,
                7,
                -1m));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.ClientId");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.CapitalAmount");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.TermInMonths");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.AnnualInterestRate");
    }

    [Fact]
    public async Task Handler_delegates_request_and_cancellation_token_to_service()
    {
        var request = CreateRequest();
        var expectedDetail = CreateDetail();
        var service = new StubLoanOriginationService
        {
            CreateResult = OperationResult<LoanDetailDto>.Success(
                expectedDetail)
        };
        var handler = new CreateLoanCommandHandler(service);
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new CreateLoanCommand(request),
            cancellationSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedDetail, result.Value);
        Assert.Same(request, service.ReceivedCreateRequest);
        Assert.Equal(
            cancellationSource.Token,
            service.ReceivedCancellationToken);
        Assert.Equal(1, service.CreateCalls);
    }

    [Fact]
    public async Task Handler_preserves_service_failure()
    {
        var service = new StubLoanOriginationService
        {
            CreateResult = OperationResult<LoanDetailDto>.Failure(
                LoanErrors.HighRiskConfirmationRequired)
        };
        var handler = new CreateLoanCommandHandler(service);

        var result = await handler.Handle(
            new CreateLoanCommand(CreateRequest()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            LoanErrors.HighRiskConfirmationRequired,
            result.Error);
        Assert.Equal(1, service.CreateCalls);
    }

    private static CreateLoanRequest CreateRequest() =>
        new(
            "client-1",
            10_000m,
            12,
            12m);

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
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero),
            []);

    private sealed class StubLoanOriginationService
        : ILoanOriginationService
    {
        public OperationResult<LoanDetailDto> CreateResult { get; init; } =
            OperationResult<LoanDetailDto>.Success(CreateDetail());

        public CreateLoanRequest? ReceivedCreateRequest { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public int CreateCalls { get; private set; }

        public Task<OperationResult<LoanDetailDto>> CreateAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedCreateRequest = request;
            ReceivedCancellationToken = cancellationToken;
            CreateCalls++;
            return Task.FromResult(CreateResult);
        }

        public Task<OperationResult<HighRiskAssessmentDto>> AssessRiskAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
