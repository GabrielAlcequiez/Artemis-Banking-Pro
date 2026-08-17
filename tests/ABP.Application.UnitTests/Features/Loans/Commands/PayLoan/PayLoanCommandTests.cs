using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.Commands.PayLoan;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Commands.PayLoan;

public sealed class PayLoanCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_payment_request_rules()
    {
        var validator = new PayLoanCommandValidator(
            new LoanPaymentRequestValidator());
        var command = new PayLoanCommand(
            new LoanPaymentRequest(
                Guid.Empty,
                Guid.Empty,
                0m,
                Guid.Empty));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.LoanId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.SourceAccountId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.Amount");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.OperationId");
    }

    [Fact]
    public async Task Handler_delegates_request_and_cancellation_token_to_service()
    {
        var request = CreateRequest();
        var expected = CreateResult(request);
        var service = new StubLoanPaymentService
        {
            Result = OperationResult<LoanPaymentResult>.Success(expected)
        };
        var handler = new PayLoanCommandHandler(service);
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new PayLoanCommand(request),
            cancellationSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
        Assert.Same(request, service.ReceivedRequest);
        Assert.Equal(cancellationSource.Token, service.ReceivedCancellationToken);
        Assert.Equal(1, service.ProcessCalls);
    }

    [Fact]
    public async Task Handler_preserves_service_failure()
    {
        var service = new StubLoanPaymentService
        {
            Result = OperationResult<LoanPaymentResult>.Failure(
                LoanErrors.AccountOwnershipRequired)
        };
        var handler = new PayLoanCommandHandler(service);

        var result = await handler.Handle(
            new PayLoanCommand(CreateRequest()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.AccountOwnershipRequired, result.Error);
        Assert.Equal(1, service.ProcessCalls);
    }

    private static LoanPaymentRequest CreateRequest() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            Guid.NewGuid());

    private static LoanPaymentResult CreateResult(
        LoanPaymentRequest request) =>
        new(
            request.LoanId,
            "123456789",
            request.SourceAccountId,
            request.Amount,
            request.Amount,
            500m,
            false,
            request.OperationId,
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    private sealed class StubLoanPaymentService : ILoanPaymentService
    {
        public required OperationResult<LoanPaymentResult> Result { get; init; }
        public LoanPaymentRequest? ReceivedRequest { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }
        public int ProcessCalls { get; private set; }

        public Task<ClientLoanPaymentOptions> GetClientOptionsAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OperationResult<CashierLoanPaymentPreview>> PrepareCashierPaymentAsync(
            string sourceAccountNumber,
            string loanNumber,
            decimal amount,
            Guid operationId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LoanOperationResult<LoanPaymentResult>> ProcessPaymentAsync(
            LoanPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedCancellationToken = cancellationToken;
            ProcessCalls++;
            return Task.FromResult(
                new LoanOperationResult<LoanPaymentResult>(
                    Result,
                    false));
        }
    }
}
