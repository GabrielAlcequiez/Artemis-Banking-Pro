using ABP.Domain.Entities.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
    {
        public void Configure(EntityTypeBuilder<AccountTransaction> builder)
        {
            builder.ToTable("AccountTransactions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.AccountId)
                .IsRequired();

            builder.HasIndex(x => x.AccountId);
            builder.HasIndex(x => x.OperationId);

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Direction)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(x => x.OperationType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.Origin)
                .HasMaxLength(256);

            builder.Property(x => x.Beneficiary)
                .HasMaxLength(256);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(x => x.RejectionReason)
                .HasMaxLength(512);

            builder.Property(x => x.ActorUserId)
                .HasMaxLength(450);

            builder.Property(x => x.ActorRole)
                .HasMaxLength(64);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<SavingsAccount>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
