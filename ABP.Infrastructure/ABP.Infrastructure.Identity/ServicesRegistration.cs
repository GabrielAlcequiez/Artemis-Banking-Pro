using System.Text;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Security;
using ABP.Application.Interfaces.Identity;
using ABP.Application.DTOs;
using ABP.Domain.Enums;
using ABP.Domain.Settings;
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
using Newtonsoft.Json;
using ABP.Infrastructure.Identity.Seeds;
using ABP.Domain.Interfaces;
using ABP.Domain.Entities;

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
                    OnAuthenticationFailed = c =>
                    {
                        c.NoResult();
                        c.Response.StatusCode = 500;
                        c.Response.ContentType = "text/plain";
                        return c.Response.WriteAsync(c.Exception.Message.ToString());
                    },
                    OnChallenge = c =>
                    {
                        c.HandleResponse();
                        c.Response.StatusCode = 401;
                        c.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(new JwtResponseDto
                        {
                            HasError = true,
                            Error = "No está autorizado para acceder a este recurso."
                        });
                        return c.Response.WriteAsync(result);
                    },
                    OnForbidden = c =>
                    {
                        c.Response.StatusCode = 403;
                        c.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(new JwtResponseDto
                        {
                            HasError = true,
                            Error = "Acceso denegado. No tiene permisos para realizar esta acción."
                        });
                        return c.Response.WriteAsync(result);
                    }
                };
            }).AddCookie(IdentityConstants.ApplicationScheme, opt =>
            {
                opt.ExpireTimeSpan = TimeSpan.FromHours(2);
            });

            #endregion
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
                configuration);
        }
    
    }
}
