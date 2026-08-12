using ABP.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests;

public sealed class ApplicationRegistrationTests
{
    [Fact]
    public void Application_services_do_not_register_mediatr_or_pipeline_behaviors()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<ISender>());
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void Application_cqrs_registers_sender_handlers_and_validation_behavior()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddApplicationServices();
        services.AddApplicationCqrs();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ISender>());
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType.IsGenericType
                && descriptor.ServiceType.GetGenericTypeDefinition()
                    == typeof(IRequestHandler<,>));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>)
                && descriptor.ImplementationType == typeof(ValidationBehavior<,>));
    }
}
