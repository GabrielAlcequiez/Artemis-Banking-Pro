using ABP.Domain.Entities;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CreditCardOwnershipRepositoryTests
{
    [Fact]
    public async Task Client_detail_returns_only_a_card_owned_by_that_client()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreditCardOwnership_{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(options);
        context.Users.AddRange(
            CreateClient("client-1", "Ana", "Pérez", "00100000001"),
            CreateClient("client-2", "Luis", "Díaz", "00100000002"));

        var card = new CreditCard
        {
            ClientId = "client-1",
            AssignedByUserId = "admin-1",
            CardNumber = "4000000000001234",
            CvcHash = new string('a', 64),
            Limit = 1_000m,
            Debt = 200m,
            ExpirationDate = new DateOnly(2030, 8, 31),
            Status = CreditCardStatus.Active,
            RowVersion = [1]
        };
        context.CreditCards.Add(card);
        await context.SaveChangesAsync();

        context.CardConsumptions.Add(new CardConsumption
        {
            CreditCardId = card.Id,
            CommerceName = "Supermercado Demo",
            CommerceId = Guid.NewGuid(),
            Amount = 200m,
            Status = ConsumptionStatus.Approved,
            OccurredAtUtc = new DateTimeOffset(
                2026, 8, 10, 12, 0, 0, TimeSpan.Zero),
            OperationId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var repository = new CreditCardRepository(context);

        var anotherClientsResult = await repository.GetDetailsForClientAsync(
            card.Id,
            "client-2");
        var ownersResult = await repository.GetDetailsForClientAsync(
            card.Id,
            "client-1");

        Assert.Null(anotherClientsResult);
        Assert.NotNull(ownersResult);
        Assert.Equal("client-1", ownersResult.ClientId);
        Assert.Equal("************1234", ownersResult.MaskedCardNumber);
        Assert.Single(ownersResult.Consumptions);
    }

    private static User CreateClient(
        string id,
        string name,
        string lastName,
        string identification) =>
        new(id)
        {
            Name = name,
            LastName = lastName,
            Email = $"{id}@example.com",
            UserName = id,
            Identification = identification,
            Role = Roles.Client,
            IsActive = true
        };
}
