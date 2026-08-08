using System.Security.Claims;
using ABP.Application;
using ABP.Infrastructure.Identity;
using ABP.Infrastructure.Persistence;
using ABP.Shared;
using ABP.Shared.Middleware;
using ABP.WebApp.Handler;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

#region Serilog Configuration

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

#endregion

builder.Services.AddInfrastructureServicesWebApp(builder.Configuration);
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSharedServices(builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// await app.Services.RunSeedsAsync();

app.UseCorrelationId();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStaticFiles();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();

try
{
    await app.RunAsync(); 
}
finally
{
    await Log.CloseAndFlushAsync(); 
}