using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Identity.Context
{
    public class IdentityContext : IdentityDbContext<AppUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("idt");

            builder.Entity<AppUser>().ToTable("IdentityUsers");
            builder.Entity<IdentityRole>().ToTable("IdentityRoles");
            builder.Entity<IdentityUserRole<string>>().ToTable("IdentityUserRoles");
            builder.Entity<IdentityUserLogin<string>>().ToTable("IdentityUserLogins");
        }
    }
}