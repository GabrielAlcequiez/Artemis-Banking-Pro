using System.Security.Claims;
using ABP.Infrastructure.Identity;
using ABP.Shared.Middleware;
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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// await app.Services.RunSeedsAsync();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

