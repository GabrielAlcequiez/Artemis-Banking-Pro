using ABP.Domain.Entities.Lending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
    {
        public void Configure(EntityTypeBuilder<LoanInstallment> builder)
        {
            builder.ToTable("LoanInstallments", table =>
            {
                table.HasCheckConstraint("CK_LoanInstallments_Number_Positive", "[Number] > 0");
                table.HasCheckConstraint("CK_LoanInstallments_Amount_Positive", "[InstallmentAmount] > 0");
                table.HasCheckConstraint("CK_LoanInstallments_InterestAmount_NonNegative", "[InterestAmount] >= 0");
                table.HasCheckConstraint("CK_LoanInstallments_CapitalAmount_NonNegative", "[CapitalAmount] >= 0");
                table.HasCheckConstraint("CK_LoanInstallments_PendingAmount_Valid", "[PendingAmount] >= 0 AND [PendingAmount] <= [InstallmentAmount]");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.LoanId)
                .IsRequired();

            builder.Property(x => x.Number)
                .IsRequired();

            builder.Property(x => x.DueDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.InstallmentAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.InterestAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.CapitalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.PendingAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.PaymentStatus)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.IsLate)
                .IsRequired();

            builder.HasOne(x => x.Loan)
                .WithMany(x => x.Installments)
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => new { x.LoanId, x.Number })
                .IsUnique();

            builder.HasIndex(x => new { x.DueDate, x.PaymentStatus, x.IsLate });

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
