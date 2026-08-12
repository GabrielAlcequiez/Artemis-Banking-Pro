using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class CardPaymentConfiguration : IEntityTypeConfiguration<CardPayment>
    {
        public void Configure(EntityTypeBuilder<CardPayment> builder)
        {
            builder.ToTable("CardPayments", t =>
            {
                t.HasCheckConstraint("CK_CardPayments_RequestedAmount_Positive", "[RequestedAmount] > 0");
                t.HasCheckConstraint(
                    "CK_CardPayments_EffectiveAmount_Valid",
                    "([Status] = 'Approved' AND [EffectiveAmount] > 0) OR ([Status] = 'Rejected' AND [EffectiveAmount] >= 0)");
                t.HasCheckConstraint("CK_CardPayments_OperationId_NotEmpty", "[OperationId] <> '00000000-0000-0000-0000-000000000000'");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.RequestedAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.EffectiveAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ActorUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.PaidAtUtc)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.FailureCode)
                .HasMaxLength(100);

            builder.Property(x => x.FailureDescription)
                .HasMaxLength(500);

            builder.HasOne<CreditCard>()
                .WithMany()
                .HasForeignKey(x => x.CreditCardId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<SavingsAccount>()
                .WithMany()
                .HasForeignKey(x => x.SourceAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => x.OperationId)
                .IsUnique();

            builder.HasIndex(x => new { x.CreditCardId, x.PaidAtUtc });

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
