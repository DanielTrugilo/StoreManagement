// StoreManagementApi.Tests/CustomWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using System.Linq;
using System.Threading;

namespace StoreManagementAPI.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private static int _databaseCounter = 0;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all Entity Framework related services more comprehensively
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType != null && d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                    (d.ServiceType != null && d.ServiceType.Name.Contains("DbContext")) ||
                    (d.ImplementationType?.Name.Contains("SqlServer") == true) ||
                    (d.ImplementationType?.Assembly?.GetName().Name?.Contains("SqlServer") == true))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database with a unique name for each test class instance
                var databaseName = $"InMemoryDbForTesting_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid()}";
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    options.EnableSensitiveDataLogging();
                    // Important: Don't use any SQL Server specific options
                }, ServiceLifetime.Scoped);

                // Build service provider and seed immediately
                var serviceProvider = services.BuildServiceProvider();
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    SeedDatabase(context);
                }
            });

            builder.UseEnvironment("Testing");
        }

        public void ResetDatabase()
        {
            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // Clear and reseed the database
                if (context.Stores.Any())
                {
                    context.Stores.RemoveRange(context.Stores);
                }
                if (context.Companies.Any())
                {
                    context.Companies.RemoveRange(context.Companies);
                }
                context.SaveChanges();

                SeedDatabase(context);
            }
        }

        private void SeedDatabase(ApplicationDbContext context)
        {
            try
            {
                // Ensure database is created
                context.Database.EnsureCreated();

                // Clear existing data to avoid conflicts
                if (context.Stores.Any())
                {
                    context.Stores.RemoveRange(context.Stores);
                }
                if (context.Companies.Any())
                {
                    context.Companies.RemoveRange(context.Companies);
                }
                context.SaveChanges();

                // Add companies
                var companyA = new Company
                {
                    Id = Guid.Parse("40a0d0a0-e1f2-3456-7890-000000000001"),
                    Name = "Company A"
                };
                var companyB = new Company
                {
                    Id = Guid.Parse("50a0d0a0-e1f2-3456-7890-000000000002"),
                    Name = "Company B"
                };

                context.Companies.AddRange(companyA, companyB);
                context.SaveChanges();

                // Add stores
                var stores = new[]
                {
                    new Store
                    {
                        Id = Guid.Parse("60a0d0a0-e1f2-3456-7890-000000000001"),
                        Name = "Store 1A",
                        Address = "123 Main St",
                        CompanyId = companyA.Id
                    },
                    new Store
                    {
                        Id = Guid.Parse("70a0d0a0-e1f2-3456-7890-000000000002"),
                        Name = "Store 2A",
                        Address = "456 Oak Ave",
                        CompanyId = companyA.Id
                    },
                    new Store
                    {
                        Id = Guid.Parse("80a0d0a0-e1f2-3456-7890-000000000003"),
                        Name = "Store 1B",
                        Address = "789 Pine Rd",
                        CompanyId = companyB.Id
                    }
                };

                context.Stores.AddRange(stores);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking test setup
                Console.WriteLine($"Error seeding database: {ex.Message}");
                throw;
            }
        }
    }
}