using ABP.Domain.Entities;
using ABP.Domain.Entities.CreditCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
    {
        public void Configure(EntityTypeBuilder<CreditCard> builder)
        {
            builder.ToTable("CreditCards", t =>
            {
                t.HasCheckConstraint("CK_CreditCards_PAN_16Digits", "LEN([PAN]) = 16 AND [PAN] NOT LIKE '%[^0-9]%'");
                t.HasCheckConstraint("CK_CreditCards_Limit_Positive", "[Limit] > 0");
                t.HasCheckConstraint("CK_CreditCards_Debt_NonNegative", "[Debt] >= 0");
                t.HasCheckConstraint("CK_CreditCards_Debt_LessThanOrEqualToLimit", "[Debt] <= [Limit]");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CardNumber)
                .HasColumnName("PAN")
                .HasColumnType("varchar(16)")
                .HasMaxLength(16)
                .IsRequired();

            builder.HasIndex(x => x.CardNumber)
                .IsUnique();

            builder.Property(x => x.CvcHash)
                .HasColumnType("char(64)")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.Limit)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Debt)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ExpirationDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.ClientId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.AssignedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.HasIndex(x => new { x.ClientId, x.Status });

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
