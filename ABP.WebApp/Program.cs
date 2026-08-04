using ABP.Infrastructure.Identity;
using ABP.Infrastructure.Persistence;
using ABP.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

#region Serilog Configuration

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

#endregion

builder.Services.AddInfrastructureIdentityServices(builder.Configuration);
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddSharedServices(builder.Configuration);
builder.Services.AddControllersWithViews();

var app = builder.Build();

// await app.Services.RunSeedsAsync();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

try
{
    await app.RunAsync(); 
}
finally
{
    await Log.CloseAndFlushAsync(); 
}