using ABP.Domain.Entities.Lending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.ToTable("Loans", table =>
            {
                table.HasCheckConstraint("CK_Loans_Number_9Digits", "LEN([LoanNumber]) = 9 AND [LoanNumber] NOT LIKE '%[^0-9]%'");
                table.HasCheckConstraint("CK_Loans_Capital_Positive", "[Capital] > 0");
                table.HasCheckConstraint("CK_Loans_PendingAmount_NonNegative", "[PendingAmount] >= 0");
                table.HasCheckConstraint("CK_Loans_AnnualInterestRate_NonNegative", "[AnnualInterestRate] >= 0");
                table.HasCheckConstraint("CK_Loans_TermInMonths_Allowed", "[TermInMonths] IN (6, 12, 18, 24, 30, 36, 42, 48, 54, 60)");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.ClientId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.LoanNumber)
                .HasColumnType("varchar(9)")
                .HasMaxLength(9)
                .IsRequired();

            builder.Property(x => x.Capital)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.PendingAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.AnnualInterestRate)
                .HasPrecision(9, 4)
                .IsRequired();

            builder.Property(x => x.TermInMonths)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.AssignedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.HasOne(x => x.AssignedByUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => x.LoanNumber)
                .IsUnique();

            builder.HasIndex(x => x.ClientId)
                .IsUnique()
                .HasFilter("[Status] = 'Active'");

            builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });

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
