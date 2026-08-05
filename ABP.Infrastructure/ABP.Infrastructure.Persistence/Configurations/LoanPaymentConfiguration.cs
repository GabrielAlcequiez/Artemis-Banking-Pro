using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.Lending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
    {
        public void Configure(EntityTypeBuilder<LoanPayment> builder)
        {
            builder.ToTable("LoanPayments", table =>
            {
                table.HasCheckConstraint("CK_LoanPayments_EffectiveAmount_Positive", "[EffectiveAmount] > 0");
                table.HasCheckConstraint("CK_LoanPayments_OperationId_NotEmpty", "[OperationId] <> '00000000-0000-0000-0000-000000000000'");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.LoanId)
                .IsRequired();

            builder.Property(x => x.SourceAccountId)
                .IsRequired();

            builder.Property(x => x.EffectiveAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ActorUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.PaidAtUtc)
                .IsRequired();

            builder.Property(x => x.OperationId)
                .IsRequired();

            builder.HasOne(x => x.Loan)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.SourceAccount)
                .WithMany()
                .HasForeignKey(x => x.SourceAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => x.OperationId)
                .IsUnique();

            builder.HasIndex(x => new { x.LoanId, x.PaidAtUtc });

            builder.HasIndex(x => new { x.SourceAccountId, x.PaidAtUtc });

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
