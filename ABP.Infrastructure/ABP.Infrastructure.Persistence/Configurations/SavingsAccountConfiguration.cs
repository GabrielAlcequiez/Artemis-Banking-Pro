using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class SavingsAccountConfiguration : IEntityTypeConfiguration<SavingsAccount>
    {
        public void Configure(EntityTypeBuilder<SavingsAccount> builder)
        {
            builder.ToTable("SavingsAccounts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.OwnerUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.AccountNumber)
                .HasMaxLength(9)
                .IsRequired();

            builder.HasIndex(x => x.AccountNumber)
                .IsUnique();

            builder.HasIndex(x => x.OwnerUserId);

            builder.Property(x => x.Balance)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.CreatedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.LastModifiedByUserId)
                .HasMaxLength(450);
        }
    }
}
