using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Functions.LoanDelinquency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Functions.UnitTests.LoanDelinquency;

public sealed class LoanDelinquencyFunctionTests
{
    [Fact]
    public async Task RunAsync_UsesConfiguredBankingDate()
    {
        var bankingDate = new DateOnly(2026, 8, 16);
        var service = new RecordingLoanDelinquencyService();
        var function = CreateFunction(service, bankingDate);

        await function.RunAsync(
            new TimerInfo(),
            CancellationToken.None);

        Assert.Equal(1, service.CallCount);
        Assert.Equal(bankingDate, service.ReceivedBankingDate);
    }

    [Fact]
    public async Task RunAsync_WhenTimerIsPastDue_StillUpdatesDelinquency()
    {
        var service = new RecordingLoanDelinquencyService();
        var function = CreateFunction(
            service,
            new DateOnly(2026, 8, 16));

        await function.RunAsync(
            new TimerInfo { IsPastDue = true },
            CancellationToken.None);

        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task RunAsync_WhenServiceFails_PropagatesException()
    {
        var expectedException = new InvalidOperationException(
            "Database unavailable.");
        var service = new RecordingLoanDelinquencyService
        {
            ExceptionToThrow = expectedException
        };
        var function = CreateFunction(
            service,
            new DateOnly(2026, 8, 16));

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => function.RunAsync(
                new TimerInfo(),
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
    }

    private static LoanDelinquencyFunction CreateFunction(
        ILoanDelinquencyService service,
        DateOnly bankingDate) =>
        new(
            service,
            new StubClock(bankingDate),
            NullLogger<LoanDelinquencyFunction>.Instance);

    private sealed class RecordingLoanDelinquencyService
        : ILoanDelinquencyService
    {
        public int CallCount { get; private set; }

        public DateOnly? ReceivedBankingDate { get; private set; }

        public Exception? ExceptionToThrow { get; init; }

        public Task<int> UpdateDelinquencyAsync(
            DateOnly bankingDate,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ReceivedBankingDate = bankingDate;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(2);
        }
    }

    private sealed class StubClock(DateOnly today) : IClock
    {
        public DateTimeOffset UtcNow =>
            new(today, TimeOnly.MinValue, TimeSpan.Zero);

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => today;
    }
}
