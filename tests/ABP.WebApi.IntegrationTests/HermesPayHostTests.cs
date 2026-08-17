using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ABP.Application.Features.Commerce.Services.Interfaces;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;
using ABP.Application.Features.HermesPay.Services.Implementations;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.WebApi.IntegrationTests;

public sealed class HermesPayHostTests(
    CreditCardsWebApplicationFactory factory)
    : IClassFixture<CreditCardsWebApplicationFactory>
{
    [Fact]
    public void Host_resolves_hermes_query_authorizer_and_repository()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetRequiredService<
            IRequestHandler<GetHermesTransactionsQuery, ABP.Application.Common.OperationResult<HermesTransactionsPageDto>>>());
        Assert.NotNull(services.GetRequiredService<
            IRequestHandler<ProcessHermesPaymentCommand, ABP.Application.Common.OperationResult<ABP.Application.Common.FinancialOperationReceipt>>>());
        Assert.IsType<CommerceAuthorizationResolverService>(
            services.GetRequiredService<ICommerceAuthorizationResolverService>());
        Assert.IsType<HermesTransactionRepository>(
            services.GetRequiredService<IHermesTransactionRepository>());
    }

    [Fact]
    public async Task Route_requires_jwt_and_an_allowed_role()
    {
        using var anonymous = CreateClient();
        using var clientRole = CreateClient(Roles.Client);
        var commerceId = Guid.NewGuid();

        var anonymousResponse = await anonymous.GetAsync(
            $"/api/v1/Pay/get-transactions/{commerceId}");
        var forbiddenResponse = await clientRole.GetAsync(
            $"/api/v1/Pay/get-transactions/{commerceId}");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task Administrator_can_query_an_active_commerce_from_the_route()
    {
        var commerceId = await SeedCommerceAsync(
            $"commerce-{Guid.NewGuid():N}",
            true);
        using var administrator = CreateClient(Roles.Administrator);

        var response = await administrator.GetAsync(
            $"/api/v1/Pay/get-transactions/{commerceId}?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<HermesTransactionsPageDto>();
        Assert.NotNull(page);
        Assert.Equal(commerceId, page.CommerceId);
        Assert.Equal("Tienda Hermes", page.CommerceName);
        Assert.Empty(page.Data);
    }

    [Fact]
    public async Task Commerce_ignores_route_and_uses_current_database_association()
    {
        var staleClaimCommerceId = Guid.NewGuid();
        var userId = $"commerce-{Guid.NewGuid():N}";
        var associatedCommerceId = await SeedCommerceAsync(userId, true);
        using var commerceClient = CreateClient(
            Roles.Commerce,
            userId,
            staleClaimCommerceId);

        var response = await commerceClient.GetAsync(
            $"/api/v1/Pay/get-transactions/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<HermesTransactionsPageDto>();
        Assert.NotNull(page);
        Assert.Equal(associatedCommerceId, page.CommerceId);
    }

    [Fact]
    public async Task Commerce_can_process_payment_when_route_id_is_empty()
    {
        var seeded = await SeedPaymentProductsAsync();
        using var commerceClient = CreateClient(
            Roles.Commerce,
            seeded.CommerceUserId,
            Guid.NewGuid());
        commerceClient.DefaultRequestHeaders.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString());

        var response = await commerceClient.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{Guid.Empty}",
            new
            {
                cardNumber = seeded.CardNumber,
                monthExpirationCard = "08",
                yearExpirationCard = "2029",
                cvc = "123",
                transactionAmount = 100m
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Stale_commerce_jwt_is_forbidden_after_domain_user_is_inactivated()
    {
        var userId = $"commerce-{Guid.NewGuid():N}";
        var commerceId = await SeedCommerceAsync(userId, false);
        using var commerceClient = CreateClient(Roles.Commerce, userId, commerceId);
        commerceClient.DefaultRequestHeaders.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString());

        var queryResponse = await commerceClient.GetAsync(
            $"/api/v1/Pay/get-transactions/{commerceId}");
        var paymentResponse = await commerceClient.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{commerceId}",
            new
            {
                cardNumber = "4000000000009876",
                monthExpirationCard = "08",
                yearExpirationCard = "2029",
                cvc = "123",
                transactionAmount = 100m
            });

        Assert.Equal(HttpStatusCode.Forbidden, queryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, paymentResponse.StatusCode);
    }

    [Fact]
    public async Task Invalid_pagination_returns_safe_problem_details()
    {
        var commerceId = await SeedCommerceAsync(
            $"commerce-{Guid.NewGuid():N}",
            true);
        using var administrator = CreateClient(Roles.Administrator);

        var response = await administrator.GetAsync(
            $"/api/v1/Pay/get-transactions/{commerceId}?page=0&pageSize=21");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("errorCode", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cardNumber", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cvc", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Administrator_can_process_an_approved_payment_end_to_end()
    {
        var seeded = await SeedPaymentProductsAsync();
        var operationId = Guid.NewGuid();
        using var administrator = CreateClient(
            Roles.Administrator,
            seeded.AdministratorId);
        administrator.DefaultRequestHeaders.Add(
            "Idempotency-Key",
            operationId.ToString());

        var payment = new
        {
            cardNumber = seeded.CardNumber,
            monthExpirationCard = "08",
            yearExpirationCard = "2029",
            cvc = "123",
            transactionAmount = 250m
        };

        var response = await administrator.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{seeded.CommerceId}",
            payment);
        var replayResponse = await administrator.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{seeded.CommerceId}",
            payment);
        var conflictResponse = await administrator.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{seeded.CommerceId}",
            new
            {
                cardNumber = seeded.CardNumber,
                monthExpirationCard = "08",
                yearExpirationCard = "2029",
                cvc = "123",
                transactionAmount = 251m
            });
        var transactionsResponse = await administrator.GetAsync(
            $"/api/v1/Pay/get-transactions/{seeded.CommerceId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, replayResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            conflictResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, transactionsResponse.StatusCode);

        var conflictBody = await conflictResponse.Content.ReadAsStringAsync();
        var transactionsBody = await transactionsResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(seeded.CardNumber, conflictBody);
        Assert.DoesNotContain(seeded.CardNumber, transactionsBody);
        Assert.DoesNotContain("\"cvc\"", transactionsBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"cvcHash\"", transactionsBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"cardNumber\"", transactionsBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(seeded.CardNumber[^4..], transactionsBody);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var card = await context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == seeded.CardId);
        var account = await context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == seeded.AccountId);
        var consumption = await context.CardConsumptions
            .AsNoTracking()
            .SingleAsync(item => item.OperationId == operationId);
        var ledger = await context.AccountTransactions
            .AsNoTracking()
            .SingleAsync(item => item.OperationId == operationId);

        Assert.Equal(350m, card.Debt);
        Assert.Equal(750m, account.Balance);
        Assert.Equal(ConsumptionStatus.Approved, consumption.Status);
        Assert.Equal(seeded.CommerceId, consumption.CommerceId);
        Assert.Equal(seeded.AccountId, consumption.TargetAccountId);
        Assert.Equal(seeded.CardNumber[^4..], ledger.Origin);
        Assert.Equal(TransactionDirection.Credit, ledger.Direction);
        Assert.Equal(FinancialOperationType.HermesPayment, ledger.OperationType);
        Assert.Equal(TransactionStatus.Approved, ledger.Status);
    }

    [Fact]
    public async Task Insufficient_credit_is_persisted_and_replayed_without_mutation()
    {
        var seeded = await SeedPaymentProductsAsync();
        var operationId = Guid.NewGuid();
        using var administrator = CreateClient(
            Roles.Administrator,
            seeded.AdministratorId);
        administrator.DefaultRequestHeaders.Add(
            "Idempotency-Key",
            operationId.ToString());
        var payment = new
        {
            cardNumber = seeded.CardNumber,
            monthExpirationCard = "08",
            yearExpirationCard = "2029",
            cvc = "123",
            transactionAmount = 950m
        };

        var response = await administrator.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{seeded.CommerceId}",
            payment);
        var replayResponse = await administrator.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{seeded.CommerceId}",
            payment);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var card = await context.CreditCards
            .AsNoTracking()
            .SingleAsync(item => item.Id == seeded.CardId);
        var account = await context.SavingsAccounts
            .AsNoTracking()
            .SingleAsync(item => item.Id == seeded.AccountId);
        var consumption = await context.CardConsumptions
            .AsNoTracking()
            .SingleAsync(item => item.OperationId == operationId);
        var ledgerEntries = await context.AccountTransactions
            .AsNoTracking()
            .Where(item => item.OperationId == operationId)
            .ToArrayAsync();

        Assert.Equal(100m, card.Debt);
        Assert.Equal(500m, account.Balance);
        Assert.Equal(ConsumptionStatus.Rejected, consumption.Status);
        Assert.Equal("HermesPay.InsufficientCredit", consumption.FailureCode);
        Assert.Empty(ledgerEntries);
    }

    [Fact]
    public async Task Process_payment_requires_a_valid_idempotency_header()
    {
        using var administrator = CreateClient(Roles.Administrator);

        var response = await administrator.PostAsJsonAsync(
            $"/api/v1/Pay/process-payment/{Guid.NewGuid()}",
            new
            {
                cardNumber = "1589963258467598",
                monthExpirationCard = "08",
                yearExpirationCard = "2029",
                cvc = "123",
                transactionAmount = 100m
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> SeedCommerceAsync(
        string userId,
        bool userActive)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var commerce = new CommerceEntity
        {
            Name = "Tienda Hermes",
            Email = $"commerce-{Guid.NewGuid():N}@example.test",
            PhoneNumber = "8095551234",
            Rnc = Guid.NewGuid().ToString("N")[..9],
            Status = CommerceStatus.Active,
            RowVersion = [1]
        };
        context.Commerces.Add(commerce);
        await context.SaveChangesAsync();
        context.Users.Add(new User(userId)
        {
            Name = "Usuario",
            LastName = "Comercio",
            Email = $"{userId}@example.test",
            UserName = userId,
            Identification = Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString(),
            Role = Roles.Commerce,
            IsActive = userActive,
            CommerceId = commerce.Id
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return commerce.Id;
    }

    private async Task<PaymentProducts> SeedPaymentProductsAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var commerceUserId = $"commerce-{suffix}";
        var administratorId = $"admin-{suffix}";
        var clientId = $"client-{suffix}";
        var commerceId = await SeedCommerceAsync(commerceUserId, true);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cvcService = scope.ServiceProvider.GetRequiredService<ICvcService>();
        context.Users.AddRange(
            CreateUser(administratorId, Roles.Administrator, suffix[..11]),
            CreateUser(clientId, Roles.Client, suffix[11..22]));
        var account = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = commerceUserId,
            AccountNumber = Random.Shared.NextInt64(100_000_000, 999_999_999).ToString(),
            Balance = 500m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active,
            RowVersion = [1]
        };
        var card = new CreditCard
        {
            ClientId = clientId,
            AssignedByUserId = administratorId,
            CardNumber = Random.Shared
                .NextInt64(1_000_000_000_000_000, 9_999_999_999_999_999)
                .ToString(),
            CvcHash = cvcService.Hash("123"),
            Limit = 1_000m,
            Debt = 100m,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = CreditCardStatus.Active,
            RowVersion = [1]
        };
        context.SavingsAccounts.Add(account);
        context.CreditCards.Add(card);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return new PaymentProducts(
            commerceId,
            commerceUserId,
            administratorId,
            card.Id,
            card.CardNumber,
            account.Id);
    }

    private static User CreateUser(string id, Roles role, string identification) =>
        new(id)
        {
            Name = "Usuario",
            LastName = role.ToString(),
            Email = $"{id}@example.test",
            UserName = id,
            Identification = identification,
            Role = role,
            IsActive = true
        };

    private HttpClient CreateClient(
        Roles? role = null,
        string? userId = null,
        Guid? commerceId = null)
    {
        var client = factory.CreateClient();
        if (role.HasValue)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                CreditCardsWebApplicationFactory.CreateJwt(
                    role.Value.ToString(),
                    userId,
                    commerceId));
        }

        return client;
    }

    private sealed record PaymentProducts(
        Guid CommerceId,
        string CommerceUserId,
        string AdministratorId,
        Guid CardId,
        string CardNumber,
        Guid AccountId);
}
