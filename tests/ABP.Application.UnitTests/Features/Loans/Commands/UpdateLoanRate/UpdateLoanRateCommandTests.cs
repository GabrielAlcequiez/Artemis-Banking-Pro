using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.Commands.UpdateLoanRate;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;

namespace ABP.Application.UnitTests.Features.Loans.Commands.UpdateLoanRate;

public sealed class UpdateLoanRateCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_update_rate_request_rules()
    {
        var validator = new UpdateLoanRateCommandValidator(
            new UpdateLoanRateRequestValidator());
        var command = new UpdateLoanRateCommand(
            new UpdateLoanRateRequest(Guid.Empty, -1m));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.LoanId");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.AnnualInterestRate");
    }

    [Fact]
    public async Task Handler_delegates_request_and_cancellation_token_to_service()
    {
        var request = new UpdateLoanRateRequest(
            Guid.NewGuid(),
            14.5m);
        var service = new StubLoanRateService
        {
            Result = OperationResult.Success()
        };
        var handler = new UpdateLoanRateCommandHandler(service);
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new UpdateLoanRateCommand(request),
            cancellationSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(request, service.ReceivedRequest);
        Assert.Equal(
            cancellationSource.Token,
            service.ReceivedCancellationToken);
        Assert.Equal(1, service.UpdateCalls);
    }

    [Fact]
    public async Task Handler_preserves_service_failure()
    {
        var service = new StubLoanRateService
        {
            Result = OperationResult.Failure(LoanErrors.NotFound)
        };
        var handler = new UpdateLoanRateCommandHandler(service);

        var result = await handler.Handle(
            new UpdateLoanRateCommand(
                new UpdateLoanRateRequest(Guid.NewGuid(), 12m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.NotFound, result.Error);
        Assert.Equal(1, service.UpdateCalls);
    }

    private sealed class StubLoanRateService : ILoanRateService
    {
        public OperationResult Result { get; init; } =
            OperationResult.Success();

        public UpdateLoanRateRequest? ReceivedRequest { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public int UpdateCalls { get; private set; }

        public Task<OperationResult> UpdateRateAsync(
            UpdateLoanRateRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedCancellationToken = cancellationToken;
            UpdateCalls++;
            return Task.FromResult(Result);
        }
    }
}
