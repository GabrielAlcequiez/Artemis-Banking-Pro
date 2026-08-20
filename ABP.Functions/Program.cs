using ABP.Application;
using ABP.Infrastructure.Persistence;
using ABP.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Configuration.AddUserSecrets<Program>(optional: true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSerilog(Log.Logger, dispose: false);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddApplicationServices();
builder.Services.AddSharedServices(builder.Configuration);
builder.Services.AddInfrastructurePersistence(builder.Configuration);

try
{
    await builder.Build().RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
