using System.Reflection;
using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Common.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Application
{
    public static class ServicesRegistration
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            #region Services

            services.AddScoped(typeof(IGenericService<,,>), typeof(GenericService<,,>));
            services.AddScoped<ICreditCardService, CreditCardService>();
            services.AddScoped<ICardDebtReaderService, CardDebtReaderService>();
            services.AddSingleton<ICardNumberGeneratorService, CardNumberGeneratorService>();


            //services.AddScoped<Features.Accounts.Services.Interfaces.IAccountBalanceService, Features.Accounts.Services.AccountBalanceService>();
            //services.AddScoped<Features.Accounts.Services.Interfaces.IAccountLedger, Features.Accounts.Services.AccountLedger>();
            //services.AddScoped<Features.Accounts.Services.Interfaces.IMoneyTransferService, Features.Accounts.Services.MoneyTransferService>();
            //services.AddScoped<Features.Accounts.Services.Interfaces.IPrimaryAccountProvisioner, Features.Accounts.Services.PrimaryAccountProvisioner>();
            //services.AddScoped<Features.Accounts.Services.Interfaces.IBeneficiaryService, Features.Accounts.Services.BeneficiaryService>();
            //services.AddScoped<Features.Accounts.Services.Interfaces.IAccountsMetricsReader, Features.Accounts.Services.AccountsMetricsReader>();
            //services.AddScoped<Features.Accounts.Services.Interfaces.ITransactionsMetricsReader, Features.Accounts.Services.TransactionsMetricsReader>();

            // Other services here

            #endregion

            #region Validators

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            #endregion

            #region Mappers

            services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

            #endregion
        }        
    }
}
