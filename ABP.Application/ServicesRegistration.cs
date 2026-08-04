using System.Reflection;
using ABP.Application.Interfaces.Services;
using ABP.Application.Services;
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