using ABP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .ValueGeneratedNever();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);
            
            builder.HasIndex(x => x.Email)
                .IsUnique();

            // Máximo en cédula/identificacion/dni de la República Dominicana
            builder.Property(x => x.Identification)
                .IsRequired()
                .HasMaxLength(11);

            builder.HasIndex(x => x.Identification)
                .IsUnique();

            builder.Property(x => x.UserName)
                .IsRequired()
                .HasMaxLength(20);
            
            builder.HasIndex(x => x.UserName)
                .IsUnique();

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.CommerceId)
                .IsRequired(false);

            // Relaciones
            builder.HasMany(u => u.SavingsAccounts)
                .WithOne()
                .HasForeignKey(sa => sa.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.CreditCards)
                .WithOne()
                .HasForeignKey(cc => cc.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Loans)
                .WithOne()
                .HasForeignKey(l => l.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Beneficiaries)
                .WithOne()
                .HasForeignKey(b => b.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}