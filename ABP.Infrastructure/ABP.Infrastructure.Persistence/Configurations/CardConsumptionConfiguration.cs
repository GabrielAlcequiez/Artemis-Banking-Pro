using ABP.Domain.Entities.CreditCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class CardConsumptionConfiguration : IEntityTypeConfiguration<CardConsumption>
    {
        public void Configure(EntityTypeBuilder<CardConsumption> builder)
        {
            builder.ToTable("CardConsumptions", t =>
            {
                t.HasCheckConstraint("CK_CardConsumptions_Amount_Positive", "[Amount] > 0");
                t.HasCheckConstraint("CK_CardConsumptions_OperationId_NotEmpty", "[OperationId] <> '00000000-0000-0000-0000-000000000000'");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.CommerceName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.OccurredAtUtc)
                .IsRequired();

            builder.HasOne<CreditCard>()
                .WithMany()
                .HasForeignKey(x => x.CreditCardId)
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<Domain.Entities.Commerce.Commerce>()
                .WithMany()
                .HasForeignKey(x => x.CommerceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => x.OperationId)
                .IsUnique();

            builder.HasIndex(x => new { x.CreditCardId, x.OccurredAtUtc });
            builder.HasIndex(x => new { x.CommerceId, x.OccurredAtUtc });

            // Audit fields
            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.CreatedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.LastModifiedAtUtc);

            builder.Property(x => x.LastModifiedByUserId)
                .HasMaxLength(450);
        }
    }
}
