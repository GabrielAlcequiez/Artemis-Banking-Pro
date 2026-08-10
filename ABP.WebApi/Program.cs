using System.Security.Claims;
using ABP.Application;
using ABP.Infrastructure.Identity;
using ABP.Infrastructure.Persistence;
using ABP.Shared;
using ABP.Shared.Middleware;
using ABP.WebApi.Extensions;
using ABP.WebApi.Handler;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

#region Serilog Configuration

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

#endregion


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddSharedServices(builder.Configuration);
builder.Services.AddInfrastructureIdentityServicesWebApi(builder.Configuration);
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioningExtension();
builder.Services.AddSwaggerExtension();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// await app.Services.RunSeedsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerExtension(app);
    app.MapOpenApi();

    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseCorrelationId();
app.UseExceptionHandler();
app.UseRouting();
app.UseSerilogRequestLogging(o =>
{
    o.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("Endpoint", httpContext.GetEndpoint()?.DisplayName);
        diagnosticContext.Set("UserName", httpContext.User.Identity?.Name);
        diagnosticContext.Set("Role", httpContext.User.FindFirst(ClaimTypes.Role)?.Value);
    };
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    await app.RunAsync(); 
}
finally
{
    await Log.CloseAndFlushAsync(); 
}

