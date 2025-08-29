using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration.
                // This ensures we're not using the real SQL Server database connection during tests.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add ApplicationDbContext using an in-memory database for testing.
                // Each test run gets a fresh, isolated database.
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database contexts.
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                    // Ensure the database is created in memory.
                    db.Database.EnsureCreated();

                    // Seed the database with test data (optional, but good for consistent tests).
                    // This data will be available for all tests using this factory.
                    if (!db.Companies.Any())
                    {
                        db.Companies.Add(new Company { Id = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000001"), Name = "Company A" });
                        db.Companies.Add(new Company { Id = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000002"), Name = "Company B" });
                        db.SaveChanges();
                    }

                    if (!db.Stores.Any())
                    {
                        db.Stores.Add(new Store { Id = Guid.Parse("s0a0d0a0-e1f2-3456-7890-000000000001"), Name = "Store 1A", Address = "123 Main St", CompanyId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000001") });
                        db.Stores.Add(new Store { Id = Guid.Parse("s0a0d0a0-e1f2-3456-7890-000000000002"), Name = "Store 2A", Address = "456 Oak Ave", CompanyId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000001") });
                        db.Stores.Add(new Store { Id = Guid.Parse("s0a0d0a0-e1f2-3456-7890-000000000003"), Name = "Store 1B", Address = "789 Pine Rd", CompanyId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000002") });
                        db.SaveChanges();
                    }
                }
            });
        }
    }
}