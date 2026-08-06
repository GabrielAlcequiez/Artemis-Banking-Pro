using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Temporary;
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
            services.AddScoped<IUserRepository, UserRepository>();

            #endregion

            #region TEMPORAL - Contratos de P2 pendientes de implementación por su propietario.
            // Al entregar P2: eliminar las siguientes líneas y registrar sus implementaciones reales.
            services.AddScoped<IFinancialIdentifierGenerator, FinancialIdentifierGenerator>();
            services.AddScoped<IPrimaryAccountProvisioner, PrimaryAccountProvisioner>();
            #endregion
        }
    }
}
