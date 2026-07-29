using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Security;
using ABP.Application.Interfaces.Identity;
using ABP.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.Identity
{
    public static class ServicesRegistration
    {
        private static readonly HashSet<string> AllowedWebRoles =
        [
            Roles.Administrator.ToString(),
            Roles.Cashier.ToString(),
            Roles.Client.ToString()
        ];

        public static IServiceCollection AddInfrastructureIdentityServices(
            this IServiceCollection services,
            IConfiguration config)
        {
            GeneralContextConfiguration(services, config);

            #region Identity Configuration

            services.ConfigureOptions<ConfigureIdentityOptions>();

            services.AddIdentityCore<AppUser>()
                .AddRoles<IdentityRole>()
                .AddSignInManager()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<PasswordResetTokenProvider<AppUser>>(
                    IdentityTokenProviderNames.PasswordReset);

            services.Configure<DataProtectionTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromHours(2);
            });

            services.Configure<PasswordResetTokenProviderOptions>(opt =>
            {
                opt.Name = IdentityTokenProviderNames.PasswordReset;
                opt.TokenLifespan = TimeSpan.FromMinutes(30);
            });

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IAccountTokenService, AccountTokenService>();
            
            services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = IdentityConstants.ApplicationScheme;
                opt.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                opt.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            }).AddCookie(IdentityConstants.ApplicationScheme, opt =>
            {
                opt.Cookie.HttpOnly = true;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                opt.Cookie.SameSite = SameSiteMode.Lax;
                opt.ExpireTimeSpan = TimeSpan.FromHours(2);
                opt.LoginPath = "/Account/Login";
                opt.AccessDeniedPath = "/Account/AccessDenied";
                opt.SlidingExpiration = true;

                opt.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var validator = context.HttpContext.RequestServices.GetRequiredService<ISecurityStampValidator>();
                        await validator.ValidateAsync(context);

                        if (context.Principal?.Identity?.IsAuthenticated != true)
                        {
                            return;
                        }

                        if (context.Principal is null)
                        {
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                        var userId = userManager.GetUserId(context.Principal);

                        if (userId is null)
                        {
                            await RejectPrincipalAsync(context);
                            return;
                        }

                        var appUser = await userManager.FindByIdAsync(userId);
                        if (appUser is null || !appUser.IsActive)
                        {
                            await RejectPrincipalAsync(context);
                            return;
                        }

                        var roles = await userManager.GetRolesAsync(appUser);
                        if (roles.Count != 1 || !AllowedWebRoles.Contains(roles[0]))
                        {
                            await RejectPrincipalAsync(context);
                        }
                    },
                    OnRedirectToLogin = context =>
                    {
                        var redirectUri = QueryHelpers.AddQueryString(
                            context.RedirectUri,
                            "reason",
                            "unauthorized");

                        context.Response.Redirect(redirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

            #endregion

            return services;
        }

        private static void GeneralContextConfiguration(IServiceCollection services, IConfiguration config)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

            string connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;

            services.AddDbContext<IdentityContext>((serviceProvider, opt) =>
            {
                if (isDevelopment)
                {
                    opt.EnableSensitiveDataLogging();
                }
                opt.UseSqlServer(
                    connectionString,
                    m => m.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName)
                );
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);
        }

        private static async Task RejectPrincipalAsync(
            CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);
        }
    }
}
