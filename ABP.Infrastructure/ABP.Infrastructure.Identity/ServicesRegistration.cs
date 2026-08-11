using System.Text;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Security;
using ABP.Domain.Enums;
using ABP.Domain.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using ABP.Infrastructure.Identity.Seeds;
using ABP.Domain.Interfaces;
using ABP.Domain.Entities;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Infrastructure.Identity.Services;

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

        public static IServiceCollection AddInfrastructureServicesWebApp(
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
            services.AddScoped<IBaseAccountService, BaseAccountService>();
            services.AddScoped<IAccountServiceForWebApp, AccountServiceForWebApp>();
            
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

        public static void AddInfrastructureIdentityServicesWebApi(this IServiceCollection services, IConfiguration config)
        {
            GeneralContextConfiguration(services, config);

            services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
            #region Jwt Authentication

            services.AddIdentityCore<AppUser>()
                .AddSignInManager()
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();


            services.Configure<DataProtectionTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromHours(2);
            });

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
                opt.RequireHttpsMetadata = false;
                opt.SaveToken = false;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    ValidIssuer = config["JwtSettings:Issuer"],
                    ValidAudience = config["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"] ?? ""))
                };
opt.Events = new JwtBearerEvents()
            {
                OnAuthenticationFailed = async context =>
                {
                    context.NoResult();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";

                    var problem = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "No tiene autorización para acceder a este recurso.",
                        Instance = context.Request.Path
                    };

                    var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    if (env.EnvironmentName == "Development")
                        problem.Extensions["exception"] = context.Exception.Message;

                    if (!string.IsNullOrEmpty(context.HttpContext.TraceIdentifier))
                        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    await context.Response.WriteAsJsonAsync(problem);
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";

                    var problem = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "No tiene autorización para acceder a este recurso.",
                        Instance = context.Request.Path
                    };

                    if (!string.IsNullOrEmpty(context.HttpContext.TraceIdentifier))
                        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    await context.Response.WriteAsJsonAsync(problem);
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";

                    var problem = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                        Title = "Forbidden",
                        Status = StatusCodes.Status403Forbidden,
                        Detail = "Acceso denegado. No tiene permisos para utilizar este recurso.",
                        Instance = context.Request.Path
                    };

                    if (!string.IsNullOrEmpty(context.HttpContext.TraceIdentifier))
                        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    await context.Response.WriteAsJsonAsync(problem);
                }
            };
            }).AddCookie(IdentityConstants.ApplicationScheme, opt =>
            {
                opt.ExpireTimeSpan = TimeSpan.FromHours(2);
            });

            #endregion

            services.AddScoped<IBaseAccountService, BaseAccountService>();
            services.AddScoped<IAccountTokenService, AccountTokenService>();
            services.AddScoped<IAccountServiceForWebApi, AccountServiceForWebApi>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
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
    
        public static async Task RunSeedsAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            await DefaultUserRoles.SeedRolesAsync(roleManager);
            await DefaultUsers.SeedDefaultUsersAsync(
                userManager,
                services.GetRequiredService<IGenericRepository<User, string>>(),
                services.GetRequiredService<IUnitOfWork>(),
                services.GetRequiredService<IPrimaryAccountProvisioner>(),
                configuration);
        }
    
    }
}
