using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ABP.Application.Behaviors;
using ABP.Application.Common;
using ABP.Application.Exceptions;
using ABP.Application.Features.CreditCards.Commands.CreateCreditCard;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ABP.WebApi.IntegrationTests;

public sealed class CreditCardsHostTests(
    CreditCardsWebApplicationFactory factory)
    : IClassFixture<CreditCardsWebApplicationFactory>
{
    [Fact]
    public void Host_resolves_sender_handlers_and_validation_behavior()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetRequiredService<ISender>());
        Assert.NotNull(
            services.GetRequiredService<
                IRequestHandler<CreateCreditCardCommand, OperationResult<Guid>>>());

        var behaviors = services.GetServices<
            IPipelineBehavior<CreateCreditCardCommand, OperationResult<Guid>>>();
        Assert.Contains(
            behaviors,
            behavior => behavior is ValidationBehavior<
                CreateCreditCardCommand,
                OperationResult<Guid>>);
    }

    [Fact]
    public async Task Request_without_jwt_returns_unauthorized_problem_details()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/credit-card");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemAsync(response, 401, "No autorizado");
    }

    [Fact]
    public async Task Client_role_returns_forbidden_problem_details()
    {
        using var client = CreateClient(Roles.Client);

        var response = await client.GetAsync("/api/credit-card");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertProblemAsync(response, 403, "Acceso denegado");
    }

    [Fact]
    public async Task Administrator_role_can_execute_cards_query_through_mediatr()
    {
        using var client = CreateClient(Roles.Administrator);

        var response = await client.GetAsync("/api/credit-card");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_command_is_rejected_by_validation_behavior_in_spanish()
    {
        using var client = CreateClient(Roles.Administrator);

        var response = await client.PostAsJsonAsync(
            "/api/credit-card",
            new { clientId = string.Empty, creditLimit = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;
        Assert.Equal("Solicitud inválida", root.GetProperty("title").GetString());
        Assert.Equal(
            "Uno o más datos proporcionados no son válidos.",
            root.GetProperty("detail").GetString());
        Assert.True(root.GetProperty("errors").TryGetProperty("Request.ClientId", out _));
        Assert.True(root.GetProperty("errors").TryGetProperty("Request.OperationId", out _));
        Assert.False(root.TryGetProperty("errorCode", out _));
    }

    [Fact]
    public async Task Financial_concurrency_is_returned_as_conflict_problem_details()
    {
        using var conflictFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUnitOfWork>();
                services.AddScoped<IUnitOfWork, ThrowingConcurrencyUnitOfWork>();
            }));
        using var client = CreateClient(
            conflictFactory,
            Roles.Administrator);
        var cardId = await SeedCardAsync(conflictFactory.Services);

        var response = await client.PatchAsJsonAsync(
            $"/api/credit-card/{cardId}/limit",
            new { creditLimit = 2_000m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemAsync(response, 409, "Conflicto");
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
                    CreditCardsWebApplicationFactory.CreateJwt(role.Value.ToString()));
        }

        return client;
    }

    private static async Task<Guid> SeedCardAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var card = new CreditCard
        {
            ClientId = "client-conflict",
            AssignedByUserId = "admin-conflict",
            CardNumber = $"4{Random.Shared.NextInt64(0, 999_999_999_999_999):D15}",
            CvcHash = new string('a', 64),
            Limit = 1_000m,
            Debt = 100m,
            ExpirationDate = new DateOnly(2030, 12, 31),
            Status = CreditCardStatus.Active,
            RowVersion = [1]
        };
        context.CreditCards.Add(card);
        await context.SaveChangesAsync();
        return card.Id;
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        int status,
        string title)
    {
        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.Equal(title, root.GetProperty("title").GetString());
        Assert.False(root.TryGetProperty("errorCode", out _));
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private sealed class ThrowingConcurrencyUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new FinancialConcurrencyException(
                new DbUpdateConcurrencyException("detalle interno EF"));
        }
    }
}
