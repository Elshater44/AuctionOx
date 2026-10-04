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

            // Seed Default Regular User
            var regularEmail = "user@auctionox.com";
            var regularUser = await userManager.FindByEmailAsync(regularEmail);

            if (regularUser == null)
            {
                var newUser = new AuctionOx.Models.ApplicationUser
                {
                    UserName = regularEmail,
                    Email = regularEmail,
                    FirstName = "Regular",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var userPassword = configuration["UserPassword"] ?? "UserPassword123!";
                var createUser = await userManager.CreateAsync(newUser, userPassword);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, "User");
                }
            }
        }
    }
}
