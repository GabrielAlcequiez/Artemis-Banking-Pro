using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Settings;
using ABP.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ABP.Shared;

public static class ServicesRegistration
{
    public static void AddSharedServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<BankingClockOptions>()
            .Bind(config.GetSection(BankingClockOptions.SectionName))
            .Validate(
                options => IsValidTimeZone(options.TimeZoneId),
                "BankingTime:TimeZoneId must identify a valid time zone.")
            .ValidateOnStart();

        services.AddSingleton<IClock, BankingClock>();
    }

    private static bool IsValidTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
