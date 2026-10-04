using System;
using System.Linq;
using AuctionOx.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionOx.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove the production SQL Server DbContext registration
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();
                
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Create isolated service provider for InMemory EF Core to avoid conflict with SqlServer provider
                var inMemoryServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.UseInternalServiceProvider(inMemoryServiceProvider);
                });

                // Ensure Identity roles exist
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                try
                {
                    DatabaseSeeder.SeedRolesAsync(scope.ServiceProvider).GetAwaiter().GetResult();
                }
                catch
                {
                    // Ignore if already seeded
                }
            });
        }
    }
}
