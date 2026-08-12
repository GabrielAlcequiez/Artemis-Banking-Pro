using ABP.Application.Exceptions;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ABP.Infrastructure.IntegrationTests.Persistence;

public sealed class UnitOfWorkConcurrencyTests
{
    [Fact]
    public async Task SaveChanges_translates_ef_concurrency_exception_without_leaking_infrastructure_details()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"UnitOfWorkConcurrency_{Guid.NewGuid():N}",
                databaseRoot)
            .Options;

        Guid cardId;
        await using (var seedContext = new AppDbContext(options))
        {
            var card = CreateCard();
            seedContext.CreditCards.Add(card);
            await seedContext.SaveChangesAsync();
            cardId = card.Id;
        }

        await using var staleContext = new AppDbContext(options);
        var staleCard = await staleContext.CreditCards.SingleAsync(
            card => card.Id == cardId);

        await using (var competingContext = new AppDbContext(options))
        {
            var competingCard = await competingContext.CreditCards.SingleAsync(
                card => card.Id == cardId);
            competingContext.CreditCards.Remove(competingCard);
            await competingContext.SaveChangesAsync();
        }

        staleCard.Limit = 2_000m;
        var unitOfWork = new UnitOfWork(staleContext);

        var exception = await Assert.ThrowsAsync<FinancialConcurrencyException>(
            () => unitOfWork.SaveChangesAsync());

        Assert.Null(exception.InnerException);
        Assert.Equal(
            "La operación no pudo completarse porque los datos fueron modificados por otro proceso. Actualice la información e intente nuevamente.",
            exception.Message);
    }

    private static CreditCard CreateCard() => new()
    {
        ClientId = "client-1",
        AssignedByUserId = "admin-1",
        CardNumber = "4000000000001234",
        CvcHash = new string('a', 64),
        Limit = 1_000m,
        Debt = 0m,
        ExpirationDate = new DateOnly(2030, 12, 31),
        Status = CreditCardStatus.Active,
        RowVersion = [1]
    };
}
