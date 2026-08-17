using ABP.Domain.Entities.CreditCards;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CreditCardPersistenceModelTests
{
    [Fact]
    public void Credit_card_model_enforces_unique_pan_and_optimistic_concurrency()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"credit-card-model-{Guid.NewGuid()}")
            .Options;
        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(CreditCard));

        Assert.NotNull(entity);
        var panIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(CreditCard.CardNumber)]));
        Assert.True(panIndex.IsUnique);

        var operationId = entity.FindProperty(nameof(CreditCard.CreationOperationId));
        Assert.NotNull(operationId);
        Assert.False(operationId.IsNullable);
        var operationIdIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(CreditCard.CreationOperationId)]));
        Assert.True(operationIdIndex.IsUnique);

        var rowVersion = entity.FindProperty(nameof(CreditCard.RowVersion));
        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }
}
