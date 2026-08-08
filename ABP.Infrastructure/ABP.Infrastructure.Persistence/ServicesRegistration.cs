using ABP.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Repositories;

namespace ABP.Infrastructure.Persistence
{
    public static class ServicesRegistration
    {
        public static void AddInfrastructurePersistence(this IServiceCollection services, IConfiguration config)
        {
            #region Context
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

            string connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;

            services.AddDbContext<AppDbContext>((serviceProvider, opt) =>
            {
                if (isDevelopment)
                {
                    opt.EnableSensitiveDataLogging();
                }
                opt.UseSqlServer(
                    connectionString,
                    m => m.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);
            #endregion

            #region Repositories
            services.AddScoped(
                typeof(IGenericRepository<,>),
                typeof(GenericRepository<,>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISavingsAccountRepository, SavingsAccountRepository>();
            services.AddScoped<IAccountTransactionRepository, AccountTransactionRepository>();
            services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();

            #endregion
        }
    }
}
