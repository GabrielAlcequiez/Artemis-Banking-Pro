using System.Reflection;
using ABP.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.IntegrationTests;

public sealed class ControllerRouteConventionTests
{
    [Fact]
    public void All_api_controllers_use_the_versioned_base_route()
    {
        var controllers = new[]
        {
            typeof(AccountController),
            typeof(UsersController),
            typeof(CommerceController),
            typeof(CreditCardsController),
            typeof(LoansController),
            typeof(SavingsAccountsController),
            typeof(PayController)
        };

        foreach (var controller in controllers)
        {
            Assert.Equal(typeof(BaseApiController), controller.BaseType);
            Assert.Equal(
                "api/v{version:apiVersion}/[controller]",
                controller.GetCustomAttributes<RouteAttribute>(inherit: true).Single().Template);
            Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        }
    }
}
