using ABP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class FinancialIdentifierConfiguration : IEntityTypeConfiguration<FinancialIdentifier>
    {
        public void Configure(EntityTypeBuilder<FinancialIdentifier> builder)
        {
            builder.ToTable("FinancialIdentifiers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(9);

            builder.HasIndex(x => x.Value)
                .IsUnique();

            builder.Property(x => x.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        }
    }
}
