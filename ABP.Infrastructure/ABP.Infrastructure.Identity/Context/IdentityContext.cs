using ABP.Domain.Entities;
using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Identity.Context
{
    public class IdentityContext : IdentityDbContext<AppUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options) { }

        public DbSet<AccountToken> AccountTokens => Set<AccountToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("idt");

            builder.Entity<AppUser>().ToTable("IdentityUsers");
            builder.Entity<IdentityRole>().ToTable("IdentityRoles");
            builder.Entity<IdentityUserRole<string>>().ToTable("IdentityUserRoles");
            builder.Entity<IdentityUserLogin<string>>().ToTable("IdentityUserLogins");

            builder.Entity<AccountToken>(entity =>
            {
                entity.ToTable("AccountTokens");
                entity.HasKey(token => token.Id);

                entity.Property(token => token.Id)
                    .ValueGeneratedNever();

                entity.Property(token => token.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(token => token.Purpose)
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(token => token.TokenHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.HasIndex(token => token.TokenHash)
                    .IsUnique();

                entity.HasIndex(token => new { token.UserId, token.Purpose });

                entity.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
