using System.Security.Claims;
using System.Diagnostics;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Common;
using ABP.WebApp.Areas.Admin.Controllers;
using ABP.WebApp.Areas.Admin.ViewModels.CreditCards;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;

namespace ABP.WebApp.IntegrationTests;

public sealed class CreditCardViewsTests
{
    [Theory]
    [MemberData(nameof(ViewCases))]
    public async Task Administrative_view_renders_with_layout_and_safe_content(
        string viewName,
        object model,
        string expectedText)
    {
        var html = await RenderAsync(viewName, model);

        Assert.Contains(expectedText, html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Artemis Banking", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("4000000000001234", html, StringComparison.Ordinal);
        Assert.DoesNotContain("CvcHash", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hashed-cvc", html, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<string, object, string> ViewCases => new()
    {
        {
            "Index",
            new CreditCardIndexViewModel
            {
                Result = new CreditCardListResult(
                    new PagedResult<CreditCardSummaryDto>([CreateSummary()], 1, 20, 1),
                    CreditCardSearchStatus.ResultsFound)
            },
            "Ver detalles"
        },
        {
            "SelectClient",
            new CreditCardClientSelectionViewModel
            {
                Result = new CreditCardClientSelectionResult(
                    new PagedResult<CreditCardClientCandidateDto>([CreateClient()], 1, 20, 1),
                    150m)
            },
            "Siguiente paso"
        },
        {
            "Create",
            new CreateCreditCardViewModel
            {
                ClientId = "client-1",
                ClientFullName = "Ana Pérez",
                ClientIdentification = "00100000001",
                ClientEmail = "ana@example.com"
            },
            "La tarjeta iniciará activa"
        },
        {
            "Details",
            new CreditCardDetailViewModel { Card = CreateDetail() },
            "Supermercado Demo"
        },
        {
            "EditLimit",
            new EditCreditLimitViewModel
            {
                CreditCardId = CardId,
                MaskedCardNumber = "************1234",
                CurrentDebt = 200m,
                CreditLimit = 1_000m
            },
            "Guardar cambios"
        },
        {
            "ConfirmCancel",
            new CancelCreditCardViewModel
            {
                CreditCardId = CardId,
                LastFourDigits = "1234"
            },
            "Sí, cancelar tarjeta"
        }
    };

    private static async Task<string> RenderAsync(string viewName, object model)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var diagnosticListener = new DiagnosticListener("CreditCardViewsTests");
        services.AddSingleton<DiagnosticSource>(diagnosticListener);
        services.AddSingleton(diagnosticListener);
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(CreditCardsController).Assembly);

        await using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "Administrador"),
                        new Claim(ClaimTypes.Role, "Administrator")
                    ],
                    "Test"))
        };
        var routeData = new RouteData();
        routeData.Routers.Add(new TestRouter());
        routeData.Values["area"] = "Admin";
        routeData.Values["controller"] = "CreditCards";
        routeData.Values["action"] = viewName;
        var actionContext = new ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor());

        var viewEngine = provider.GetRequiredService<IRazorViewEngine>();
        var viewResult = viewEngine.GetView(
            executingFilePath: null,
            viewPath: $"/Areas/Admin/Views/CreditCards/{viewName}.cshtml",
            isMainPage: true);
        Assert.True(viewResult.Success, string.Join(Environment.NewLine, viewResult.SearchedLocations ?? []));

        var metadataProvider = provider.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            Model = model
        };
        var tempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());
        await using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }

    private static CreditCardSummaryDto CreateSummary() =>
        new(
            CardId,
            "************1234",
            "1234",
            "client-1",
            "Ana Pérez",
            1_000m,
            800m,
            200m,
            "08/29",
            "Activa",
            CreatedAt);

    private static CreditCardClientCandidateDto CreateClient() =>
        new("client-1", "00100000001", "Ana Pérez", "ana@example.com", 350m);

    private static CreditCardDetailDto CreateDetail() =>
        new(
            CardId,
            "************1234",
            "1234",
            "client-1",
            "Ana Pérez",
            1_000m,
            800m,
            200m,
            "08/29",
            "Activa",
            CreatedAt,
            [
                new CardConsumptionDto(
                    Guid.NewGuid(),
                    CreatedAt.AddDays(1),
                    200m,
                    "Supermercado Demo",
                    "APROBADO")
            ]);

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestRouter : IRouter
    {
        public VirtualPathData? GetVirtualPath(VirtualPathContext context) =>
            new(this, "/Admin/CreditCards");

        public Task RouteAsync(RouteContext context) => Task.CompletedTask;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ABP.WebApp";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = "Development";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static readonly Guid CardId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
}
