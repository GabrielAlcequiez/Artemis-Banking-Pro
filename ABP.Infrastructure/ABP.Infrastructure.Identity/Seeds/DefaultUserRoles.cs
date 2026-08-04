using ABP.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ABP.Infrastructure.Identity.Seeds
{
    public static class DefaultUserRoles
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new List<string> { Roles.Administrator.ToString(), Roles.Client.ToString(), Roles.Cashier.ToString(), Roles.Commerce.ToString() };
            foreach(var role in roles)
            {
                var roleExist = await roleManager.RoleExistsAsync(role);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}