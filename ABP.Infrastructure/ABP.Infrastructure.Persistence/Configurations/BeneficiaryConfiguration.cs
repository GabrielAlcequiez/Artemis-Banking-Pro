using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
    {
        public void Configure(EntityTypeBuilder<Beneficiary> builder)
        {
            builder.ToTable("Beneficiaries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.OwnerUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.BeneficiaryAccountId)
                .IsRequired();

            builder.HasIndex(x => new { x.OwnerUserId, x.BeneficiaryAccountId })
                .IsUnique();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<SavingsAccount>()
                .WithMany()
                .HasForeignKey(x => x.BeneficiaryAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
