using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ABP.Application.Behaviors;
using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;
using ABP.Application.Features.Commerce.Commands.CreateCommerce;
using ABP.Application.Features.Commerce.Queries.GetCommerces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ABP.WebApi.IntegrationTests;

public sealed class CommerceHostTests(
    CreditCardsWebApplicationFactory factory)
    : IClassFixture<CreditCardsWebApplicationFactory>
{
    [Fact]
    public void Host_resolves_commerce_handlers_and_validation_behavior()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(
            services.GetRequiredService<
                IRequestHandler<GetCommercesQuery, ABP.Domain.Common.PagedResult<
                    ABP.Application.Features.Commerce.DTOs.CommerceSummaryDto>>>());
        Assert.NotNull(
            services.GetRequiredService<
                IRequestHandler<CreateCommerceCommand, OperationResult<Guid>>>());
        Assert.Contains(
            services.GetServices<
                IPipelineBehavior<CreateCommerceCommand, OperationResult<Guid>>>(),
            behavior => behavior is ValidationBehavior<
                CreateCommerceCommand,
                OperationResult<Guid>>);
    }

    [Fact]
    public async Task Commerce_routes_enforce_jwt_and_administrator_role()
    {
        using var anonymous = CreateClient();
        using var commerceUser = CreateClient(Roles.Commerce);
        using var administrator = CreateClient(Roles.Administrator);

        var anonymousResponse = await anonymous.GetAsync("/api/v1/Commerce");
        var forbiddenResponse = await commerceUser.GetAsync("/api/v1/Commerce");
        var allowedResponse = await administrator.GetAsync("/api/v1/Commerce");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        await AssertProblemAsync(anonymousResponse, 401, "No autorizado");
        await AssertProblemAsync(forbiddenResponse, 403, "Acceso denegado");
    }

    [Fact]
    public async Task Invalid_query_and_command_are_rejected_in_spanish()
    {
        using var client = CreateClient(Roles.Administrator);

        var queryResponse = await client.GetAsync(
            "/api/v1/Commerce?page=0&pageSize=21");
        var commandResponse = await client.PostAsJsonAsync(
            "/api/v1/Commerce",
            new
            {
                name = "",
                email = "correo-invalido",
                phoneNumber = "",
                rnc = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, queryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, commandResponse.StatusCode);
        await AssertProblemAsync(queryResponse, 400, "Solicitud inválida");
        await AssertProblemAsync(commandResponse, 400, "Solicitud inválida");
    }

    [Fact]
    public async Task Missing_patch_status_returns_bad_request()
    {
        using var client = CreateClient(Roles.Administrator);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/Commerce/{Guid.NewGuid()}/status",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(
            "El campo status es requerido.",
            json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Administrator_can_execute_full_commerce_http_lifecycle()
    {
        using var lifecycleFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICommerceUserInactivationService>();
                services.AddScoped<
                    ICommerceUserInactivationService,
                    CommittingCommerceUserInactivationService>();
            }));
        using var client = CreateClient(
            lifecycleFactory,
            Roles.Administrator);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/Commerce",
            new
            {
                name = "Tienda HTTP",
                description = "Prueba de ciclo completo",
                email = $"commerce-{suffix}@example.test",
                phoneNumber = "8095551234",
                rnc = suffix[..8]
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = await ReadJsonAsync(createResponse);
        var commerceId = createdJson.RootElement.GetProperty("id").GetGuid();
        Assert.True(createdJson.RootElement.GetProperty("isActive").GetBoolean());

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/Commerce/{commerceId}",
            new
            {
                name = "Tienda HTTP Actualizada",
                description = "Datos actualizados",
                email = $"updated-{suffix}@example.test",
                phoneNumber = "8095559876",
                rnc = suffix[..8]
            });
        var statusResponse = await client.PatchAsJsonAsync(
            $"/api/v1/Commerce/{commerceId}/status",
            new { status = false });
        var detailResponse = await client.GetAsync(
            $"/api/v1/Commerce/{commerceId}");
        var listResponse = await client.GetAsync(
            "/api/v1/Commerce?status=inactivo");

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var detailJson = await ReadJsonAsync(detailResponse);
        Assert.Equal(
            "Tienda HTTP Actualizada",
            detailJson.RootElement.GetProperty("name").GetString());
        Assert.False(detailJson.RootElement.GetProperty("isActive").GetBoolean());
    }

    private HttpClient CreateClient(Roles? role = null) =>
        CreateClient(factory, role);

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> webFactory,
        Roles? role)
    {
        var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (role.HasValue)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    CreditCardsWebApplicationFactory.CreateJwt(
                        role.Value.ToString()));
        }

        return client;
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        int status,
        string title)
    {
        using var json = await ReadJsonAsync(response);
        Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.False(json.RootElement.TryGetProperty("errorCode", out _));
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private sealed class CommittingCommerceUserInactivationService(
        IUnitOfWork unitOfWork) : ICommerceUserInactivationService
    {
        public async Task InactivateAssociatedUsersAndCommitAsync(
            Guid commerceId,
            CancellationToken cancellationToken = default)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
