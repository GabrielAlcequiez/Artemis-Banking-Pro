using ABP.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace ABP.Shared.Services;

public sealed class BankingClock : IClock
{
    private readonly TimeProvider timeProvider;
    private readonly TimeZoneInfo bankingTimeZone;

    public BankingClock(
        TimeProvider timeProvider,
        IOptions<BankingClockOptions> options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        this.timeProvider = timeProvider;
        bankingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            options.Value.TimeZoneId);
    }

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateTimeOffset Now =>
        TimeZoneInfo.ConvertTime(UtcNow, bankingTimeZone);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}
