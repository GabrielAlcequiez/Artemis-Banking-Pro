using ABP.Application.Common.Interfaces.Services;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => new(
        today.Year,
        today.Month,
        today.Day,
        12,
        0,
        0,
        TimeSpan.Zero);

    public DateTimeOffset Now => UtcNow;

    public DateOnly Today => today;
}
