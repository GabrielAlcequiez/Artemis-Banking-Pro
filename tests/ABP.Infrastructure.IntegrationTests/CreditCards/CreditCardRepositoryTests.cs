using ABP.Domain.Entities;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CreditCardRepositoryTests : IAsyncLifetime
{
    #region Test setup

    private readonly string _databaseName = $"ABP_CreditCardRepoTests_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;
    private CreditCardRepository _repository = null!;

    public CreditCardRepositoryTests()
    {
        _connectionString = TestDatabase.CreateConnectionString(_databaseName);
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new CreditCardRepository(_context);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    #endregion

    #region Administrative read tests

    [Fact]
    public async Task GetByCreationOperationId_returns_the_matching_card()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetByCreationOperationIdAsync(
            seeded.ActiveNew.CreationOperationId);

        Assert.NotNull(result);
        Assert.Equal(seeded.ActiveNew.Id, result.Id);
    }

    [Fact]
    public async Task Default_search_returns_only_active_cards_in_descending_created_order()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.SearchAsync(1, 20);

        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(
            [seeded.ActiveNew.Id, seeded.ActiveOld.Id, seeded.OtherActive.Id],
            result.Data.Select(card => card.Id).ToArray());
        Assert.All(result.Data, card => Assert.Equal(CreditCardStatus.Active, card.Status));
        Assert.All(result.Data, card => Assert.DoesNotContain(card.LastFourDigits, card.MaskedCardNumber[..12]));
    }

    [Fact]
    public async Task Search_filters_out_users_that_are_not_clients()
    {
        await SeedAsync(_context);

        // Add non-client user (Admin) with a card
        var adminUser = new User("admin-1")
        {
            Name = "Admin",
            LastName = "User",
            Identification = "999",
            Email = "admin1@example.test",
            UserName = "admin1",
            IsActive = true,
            Role = Roles.Administrator
        };
        _context.Users.Add(adminUser);
        AddCard(_context, "admin-1", "4000000000009999", CreditCardStatus.Active, 1000m, 0m, DateTimeOffset.UtcNow);
        await _context.SaveChangesAsync();

        var result = await _repository.SearchAsync(1, 20, identification: "999");

        Assert.Equal(0, result.TotalRecords);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetByCardNumber_returns_existing_card()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetByCardNumberAsync("4000000000001234");

        Assert.NotNull(result);
        Assert.Equal(seeded.ActiveNew.Id, result.Id);
        Assert.Equal("client-1", result.ClientId);
    }

    [Fact]
    public async Task GetByCardNumber_returns_null_when_card_does_not_exist()
    {
        await SeedAsync(_context);

        var result = await _repository.GetByCardNumberAsync("4999999999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task CardNumberExists_returns_expected_result()
    {
        await SeedAsync(_context);

        var existingResult = await _repository.CardNumberExistsAsync("4000000000001111");
        var missingResult = await _repository.CardNumberExistsAsync("4999999999999999");

        Assert.True(existingResult);
        Assert.False(missingResult);
    }

    [Fact]
    public async Task FindClientIdByIdentification_returns_null_for_non_client_user()
    {
        await SeedAsync(_context);

        var result = await _repository.FindClientIdByIdentificationAsync("00000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindClientIdByIdentification_returns_null_when_identification_does_not_exist()
    {
        await SeedAsync(_context);

        var result = await _repository.FindClientIdByIdentificationAsync("88888888888");

        Assert.Null(result);
    }

    [Fact]
    public async Task Identification_without_status_returns_all_client_cards_with_active_group_first()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.SearchAsync(1, 20, identification: " 123 ");

        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(
            [seeded.ActiveNew.Id, seeded.ActiveOld.Id, seeded.Cancelled.Id],
            result.Data.Select(card => card.Id).ToArray());
        Assert.Equal(
            [CreditCardStatus.Active, CreditCardStatus.Active, CreditCardStatus.Cancelled],
            result.Data.Select(card => card.Status).ToArray());
    }

    [Fact]
    public async Task All_status_returns_active_cards_before_cancelled_cards()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.SearchAsync(1, 20, status: CreditCardStatusFilter.All);

        Assert.Equal(4, result.TotalRecords);
        Assert.Equal(
            [
                seeded.ActiveNew.Id,
                seeded.ActiveOld.Id,
                seeded.OtherActive.Id,
                seeded.Cancelled.Id
            ],
            result.Data.Select(card => card.Id).ToArray());
    }

    [Fact]
    public async Task Status_filter_and_page_size_are_applied()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.SearchAsync(2, 1, status: CreditCardStatusFilter.Active);

        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(seeded.ActiveOld.Id, result.Data.Single().Id);
    }

    [Fact]
    public async Task GetDetails_returns_safe_card_data_and_recent_consumptions()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetDetailsAsync(seeded.ActiveNew.Id);

        Assert.NotNull(result);
        Assert.Equal("************1234", result.MaskedCardNumber);
        Assert.Equal("1234", result.LastFourDigits);
        Assert.Equal(350m, result.AvailableCredit);
        Assert.Equal(
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            result.CreatedAt);
        Assert.Equal(
            ["AVANCE", "Supermercado"],
            result.Consumptions.Select(consumption => consumption.CommerceName).ToArray());
        Assert.Equal(
            [250m, 100m],
            result.Consumptions.Select(consumption => consumption.Amount).ToArray());
    }

    [Fact]
    public async Task GetDetails_returns_null_when_card_does_not_exist()
    {
        await SeedAsync(_context);

        var result = await _repository.GetDetailsAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetails_returns_null_when_card_owner_is_not_a_client()
    {
        await SeedAsync(_context);
        var adminCard = AddCard(
            _context,
            "admin",
            "4000000000004444",
            CreditCardStatus.Active,
            1000m,
            0m,
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero));
        await _context.SaveChangesAsync();

        var result = await _repository.GetDetailsAsync(adminCard.Id);

        Assert.Null(result);
    }

    #endregion

    #region Debt and lifecycle tests

    [Fact]
    public async Task Active_debt_query_excludes_cancelled_cards()
    {
        await SeedAsync(_context);

        var debt = await _repository.GetActiveDebtByClientIdAsync("client-1");

        Assert.Equal(175m, debt);
    }

    [Fact]
    public async Task Active_debt_query_returns_zero_when_client_has_no_active_cards()
    {
        await SeedAsync(_context);
        var clientWithoutActiveCards = new User("client-without-active-cards")
        {
            Name = "Ana",
            LastName = "Pérez",
            Identification = "789",
            Email = "ana@example.test",
            UserName = "ana",
            IsActive = true,
            Role = Roles.Client
        };
        _context.Users.Add(clientWithoutActiveCards);
        AddCard(
            _context,
            clientWithoutActiveCards.Id,
            "4000000000005555",
            CreditCardStatus.Cancelled,
            500m,
            125m,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
        await _context.SaveChangesAsync();

        var debt = await _repository.GetActiveDebtByClientIdAsync(clientWithoutActiveCards.Id);

        Assert.Equal(0m, debt);
    }

    [Fact]
    public async Task IsActiveClient_returns_true_only_for_an_existing_active_client()
    {
        // Arrange
        await SeedAsync(_context);

        var inactiveClient = new User("inactive-client")
        {
            Name = "Cliente",
            LastName = "Inactivo",
            Identification = "99999999999",
            Email = "inactive@example.test",
            UserName = "inactive-client",
            IsActive = false,
            Role = Roles.Client
        };

        _context.Users.Add(inactiveClient);
        await _context.SaveChangesAsync();

        // Act
        var activeResult =
            await _repository.IsActiveClientAsync("client-1");

        var inactiveResult =
            await _repository.IsActiveClientAsync("inactive-client");

        var administratorResult =
            await _repository.IsActiveClientAsync("admin");

        var missingResult =
            await _repository.IsActiveClientAsync("missing-client");

        // Assert
        Assert.True(activeResult);
        Assert.False(inactiveResult);
        Assert.False(administratorResult);
        Assert.False(missingResult);
    }

    [Fact]
    public async Task ClientExists_includes_inactive_clients_but_excludes_other_roles()
    {
        await SeedAsync(_context);

        var inactiveClient = new User("inactive-client")
        {
            Name = "Cliente",
            LastName = "Inactivo",
            Identification = "99999999999",
            Email = "inactive@example.test",
            UserName = "inactive-client",
            IsActive = false,
            Role = Roles.Client
        };
        _context.Users.Add(inactiveClient);
        await _context.SaveChangesAsync();

        Assert.True(await _repository.ClientExistsAsync("client-1"));
        Assert.True(await _repository.ClientExistsAsync("inactive-client"));
        Assert.False(await _repository.ClientExistsAsync("admin"));
        Assert.False(await _repository.ClientExistsAsync("missing-client"));
    }

    [Fact]
    public async Task GetForUpdate_tracks_card_and_persists_changes()
    {
        // Arrange
        var seeded = await SeedAsync(_context);
        var unitOfWork = new UnitOfWork(_context);

        // Act
        var card = await _repository.GetForUpdateAsync(
            seeded.ActiveNew.Id);

        Assert.NotNull(card);

        card.Limit = 750m;

        await unitOfWork.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var persistedCard = await _repository.GetByIdAsync(
            seeded.ActiveNew.Id);

        // Assert
        Assert.NotNull(persistedCard);
        Assert.Equal(750m, persistedCard.Limit);
    }

    #endregion

    #region Test data builders

    private static async Task<SeededCards> SeedAsync(AppDbContext context)
    {
        var client = new User("client-1")
        {
            Name = "María",
            LastName = "Gómez",
            Identification = "123",
            Email = "maria@example.test",
            UserName = "maria",
            IsActive = true,
            Role = Roles.Client
        };
        var otherClient = new User("client-2")
        {
            Name = "Pedro",
            LastName = "Díaz",
            Identification = "456",
            Email = "pedro@example.test",
            UserName = "pedro",
            IsActive = true,
            Role = Roles.Client
        };
        var adminUser = new User("admin")
        {
            Name = "Admin",
            LastName = "System",
            Identification = "00000000000",
            Email = "admin@example.test",
            UserName = "admin",
            IsActive = true,
            Role = Roles.Administrator
        };

        context.Users.AddRange(adminUser, client, otherClient);

        var supermarket = new Commerce
        {
            Name = "Supermercado",
            Email = "info@super.test",
            PhoneNumber = "8095551234",
            Rnc = "123456789",
            Status = CommerceStatus.Active
        };
        context.Commerces.Add(supermarket);

        var activeOld = AddCard(
            context,
            "client-1",
            "4000000000001111",
            CreditCardStatus.Active,
            200m,
            25m,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var activeNew = AddCard(
            context,
            "client-1",
            "4000000000001234",
            CreditCardStatus.Active,
            500m,
            150m,
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var cancelled = AddCard(
            context,
            "client-1",
            "4000000000002222",
            CreditCardStatus.Cancelled,
            300m,
            90m,
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        var otherActive = AddCard(
            context,
            "client-2",
            "4000000000003333",
            CreditCardStatus.Active,
            100m,
            10m,
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await context.SaveChangesAsync();

        AddConsumption(
            context,
            activeNew.Id,
            null,
            "",
            250m,
            new DateTimeOffset(2026, 2, 2, 0, 0, 0, TimeSpan.Zero));
        AddConsumption(
            context,
            activeNew.Id,
            supermarket.Id,
            "Supermercado",
            100m,
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        await context.SaveChangesAsync();

        return new(activeOld, activeNew, cancelled, otherActive);
    }

    private static CreditCard AddCard(
        AppDbContext context,
        string clientId,
        string cardNumber,
        CreditCardStatus status,
        decimal limit,
        decimal debt,
        DateTimeOffset createdAt)
    {
        var card = new CreditCard
        {
            ClientId = clientId,
            CardNumber = cardNumber,
            CvcHash = new string('A', 64),
            Limit = limit,
            Debt = debt,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = status,
            AssignedByUserId = "admin",
            CreationOperationId = Guid.NewGuid()
        };

        context.CreditCards.Add(card);
        context.Entry(card).Property(entity => entity.Id).CurrentValue = Guid.NewGuid();
        context.Entry(card).Property(entity => entity.CreatedAtUtc).CurrentValue = createdAt;
        return card;
    }

    private static void AddConsumption(
        AppDbContext context,
        Guid creditCardId,
        Guid? commerceId,
        string commerceName,
        decimal amount,
        DateTimeOffset occurredAt)
    {
        var consumption = new CardConsumption
        {
            CreditCardId = creditCardId,
            CommerceId = commerceId,
            CommerceName = commerceName,
            Amount = amount,
            Status = ConsumptionStatus.Approved,
            OccurredAtUtc = occurredAt,
            OperationId = Guid.NewGuid()
        };

        context.CardConsumptions.Add(consumption);
        context.Entry(consumption).Property(entity => entity.Id).CurrentValue = Guid.NewGuid();
        context.Entry(consumption).Property(entity => entity.CreatedAtUtc).CurrentValue = occurredAt;
    }

    private sealed record SeededCards(
        CreditCard ActiveOld,
        CreditCard ActiveNew,
        CreditCard Cancelled,
        CreditCard OtherActive);

    #endregion
}
