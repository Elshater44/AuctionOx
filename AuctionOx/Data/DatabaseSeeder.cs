using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AuctionOx.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AuctionOx.Models.ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Default Admin User
            var adminEmail = "admin@auctionox.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new AuctionOx.Models.ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var adminPassword = configuration["AdminPassword"] ?? "AdminPassword123!"; // fallback for local dev if not set
                var createPowerUser = await userManager.CreateAsync(newAdmin, adminPassword);
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}
