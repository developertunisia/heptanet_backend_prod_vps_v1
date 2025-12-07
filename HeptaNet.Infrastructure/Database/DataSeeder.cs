using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using HeptaNet.Domain.Models;
using HeptaNet.Domain.Constants;

namespace HeptaNet.Infrastructure.Database;

public static class DataSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var roleName in AppRoles.AllRoles)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole<int> 
                { 
                    Name = roleName 
                });
            }
        }
    }

    public static async Task SeedSuperAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        
        var superAdminEmail = "admin@heptanet.com";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        
        if (superAdmin == null)
        {
            superAdmin = new User
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            
            var result = await userManager.CreateAsync(superAdmin, "Admin@123456");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
            }
        }
    }
}
