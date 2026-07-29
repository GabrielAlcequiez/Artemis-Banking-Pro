using Microsoft.AspNetCore.Identity;

namespace ABP.Infrastructure.Identity.Entities
{
    public class AppUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
    }
}