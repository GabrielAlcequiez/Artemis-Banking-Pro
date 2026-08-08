using ABP.Application.Common.Interfaces.Services;
using ABP.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ABP.Shared.UnitTests.Services;

public sealed class BankingClockTests
{
    [Fact]
    public void Clock_uses_banking_time_zone_when_utc_is_still_previous_banking_day()
    {
        var utcNow = new DateTimeOffset(2026, 8, 8, 3, 30, 0, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(utcNow);
        var options = Options.Create(new BankingClockOptions
        {
            TimeZoneId = "America/La_Paz"
        });
        IClock clock = new BankingClock(timeProvider, options);

        Assert.Equal(utcNow, clock.UtcNow);
        Assert.Equal(new DateOnly(2026, 8, 7), clock.Today);
        Assert.Equal(TimeSpan.FromHours(-4), clock.Now.Offset);
    }

    [Fact]
    public void Shared_registration_rejects_invalid_time_zone_during_startup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BankingTime:TimeZoneId"] = "Invalid/BankingZone"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSharedServices(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
