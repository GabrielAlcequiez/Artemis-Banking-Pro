using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Settings;
using ABP.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Shared
{
    public static class ServicesRegistration
    {
        public static void AddSharedServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<EmailSettings>(config.GetSection("EmailSettings"));

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}