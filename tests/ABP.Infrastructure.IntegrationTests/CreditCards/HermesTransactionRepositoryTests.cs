using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class HermesTransactionRepositoryTests
{
    [Fact]
    public async Task Card_lookup_for_update_tracks_and_persists_debt_changes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"HermesTrackedCard_{Guid.NewGuid():N}")
            .Options;
        await using var context = new AppDbContext(options);
        var card = new CreditCard
        {
            ClientId = "client-1",
            AssignedByUserId = "admin-1",
            CardNumber = "4000000000001234",
            CvcHash = new string('a', 64),
            Limit = 1_000m,
            Debt = 100m,
            ExpirationDate = new DateOnly(2030, 12, 31),
            Status = CreditCardStatus.Active,
            RowVersion = [1]
        };
        context.CreditCards.Add(card);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new CreditCardRepository(context);

        var trackedCard = await repository.GetByCardNumberForUpdateAsync(
            card.CardNumber);
        trackedCard!.Debt += 250m;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedDebt = await context.CreditCards
            .AsNoTracking()
            .Where(item => item.Id == card.Id)
            .Select(item => item.Debt)
            .SingleAsync();
        Assert.Equal(350m, persistedDebt);
    }

    [Fact]
    public async Task GetByCommerce_filters_orders_pages_and_projects_only_last_four_digits()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"HermesTransactions_{Guid.NewGuid():N}")
            .Options;
        await using var context = new AppDbContext(options);
        var commerceId = Guid.NewGuid();
        var otherCommerceId = Guid.NewGuid();
        var card = new CreditCard
        {
            ClientId = "client-1",
            AssignedByUserId = "admin-1",
            CardNumber = "1589963258467598",
            CvcHash = new string('a', 64),
            Limit = 10_000m,
            Debt = 1_500m,
            ExpirationDate = new DateOnly(2030, 12, 31),
            Status = CreditCardStatus.Active,
            RowVersion = [1]
        };
        context.CreditCards.Add(card);
        context.CardConsumptions.AddRange(
            CreateConsumption(card.Id, commerceId, 100m, 10),
            CreateConsumption(card.Id, commerceId, 200m, 20),
            CreateConsumption(card.Id, otherCommerceId, 999m, 30));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new HermesTransactionRepository(context);

        var firstPage = await repository.GetByCommerceAsync(
            commerceId,
            page: 1,
            pageSize: 1);
        var secondPage = await repository.GetByCommerceAsync(
            commerceId,
            page: 2,
            pageSize: 1);

        Assert.Equal(2, firstPage.TotalRecords);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(200m, Assert.Single(firstPage.Data).Amount);
        Assert.Equal("7598", Assert.Single(firstPage.Data).CardLastFourDigits);
        Assert.Equal(100m, Assert.Single(secondPage.Data).Amount);
    }

    private static CardConsumption CreateConsumption(
        Guid cardId,
        Guid commerceId,
        decimal amount,
        int minute) =>
        new()
        {
            CreditCardId = cardId,
            CommerceId = commerceId,
            CommerceName = "Tienda Hermes",
            RequestedAmount = amount,
            Amount = amount,
            Status = ConsumptionStatus.Approved,
            OccurredAtUtc = new DateTimeOffset(2026, 8, 12, 14, minute, 0, TimeSpan.Zero),
            OperationId = Guid.NewGuid()
        };
}
