using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Auditing;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Temporary;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.Infrastructure.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.Persistence
{
    public static class ServicesRegistration
    {
        public static void AddInfrastructurePersistence(this IServiceCollection services, IConfiguration config)
        {
            #region Context
            string connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;

            services.AddScoped<AuditableEntityInterceptor>();
            services.AddDbContext<AppDbContext>((serviceProvider, opt) =>
            {
                opt.UseSqlServer(
                    connectionString,
                    m => m.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
                opt.AddInterceptors(
                    serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);
            #endregion

            #region Repositories
            services.AddScoped(
                typeof(IGenericRepository<,>),
                typeof(GenericRepository<,>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICreditCardRepository, CreditCardRepository>();
            services.AddScoped<ICvcService, CvcService>();
            services.AddScoped<IUserRepository, UserRepository>();

            #endregion

            #region TEMPORAL - Contratos de P2 pendientes de implementación por su propietario.
            // Al entregar P2: eliminar las siguientes líneas y registrar sus implementaciones reales.
            services.AddScoped<IFinancialIdentifierGenerator, FinancialIdentifierGenerator>();
            services.AddScoped<IPrimaryAccountProvisioner, PrimaryAccountProvisioner>();
            services.AddScoped<ISavingsAccountRepository, SavingsAccountRepository>();
            services.AddScoped<IAccountBalanceService, AccountBalanceService>();
            services.AddScoped<IAccountLedger, AccountLedger>();
            #endregion

            services.AddSingleton<IValidateOptions<CvcHasherOptions>, CvcHasherOptionsValidator>();
            services.AddOptions<CvcHasherOptions>()
                .Bind(config.GetSection(CvcHasherOptions.SectionName))
                .ValidateOnStart();
        }
    }
}
