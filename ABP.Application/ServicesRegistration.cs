using System.Reflection;
using ABP.Application.Behaviors;
using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Common.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Services.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Accounts.Services;

namespace ABP.Application
{
    public static class ServicesRegistration
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            #region Services

            services.AddScoped(typeof(IGenericService<,,>), typeof(GenericService<,,>));
            services.AddScoped<ICustomerDebtService, CustomerDebtService>();
            services.AddScoped<ICreditCardService, CreditCardService>();
            services.AddScoped<ICreditCardClientSelectionService, CreditCardClientSelectionService>();
            services.AddSingleton<ICardNumberGeneratorService, CardNumberGeneratorService>();
            services.AddScoped<IAmortizationCalculator, AmortizationCalculator>();
            services.AddScoped<ILoanService, LoanService>();
            services.AddScoped<ILoanRateService, LoanRateService>();
            services.AddScoped<ILoanDelinquencyService, LoanDelinquencyService>();
            services.AddScoped<IAccountBalanceService, AccountBalanceService>();
            services.AddScoped<IAccountLedger, AccountLedger>();
            services.AddScoped<IMoneyTransferService, MoneyTransferService>();
            services.AddScoped<IPrimaryAccountProvisioner, PrimaryAccountProvisioner>();
            services.AddScoped<IBeneficiaryService, BeneficiaryService>();
            services.AddScoped<IAccountsMetricsReader, AccountsMetricsReader>();
            services.AddScoped<ITransactionsMetricsReader, TransactionsMetricsReader>();

            // Other services here

            #endregion

            #region Validators

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // pipeline behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            #endregion

            #region Mappers

            services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

            #endregion
        }        
    }
}
