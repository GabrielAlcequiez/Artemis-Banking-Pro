using System.Reflection;
using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Common.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Services.Interfaces;
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
            services.AddScoped<IAmortizationCalculator, AmortizationCalculator>();
            services.AddScoped<ILoanDebtReader, LoanDebtReader>();
            services.AddScoped<ILoanRateService, LoanRateService>();
            services.AddScoped<ILoanDelinquencyService, LoanDelinquencyService>();

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
