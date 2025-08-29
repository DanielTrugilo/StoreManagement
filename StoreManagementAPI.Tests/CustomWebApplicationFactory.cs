// StoreManagementApi.Tests/CustomWebApplicationFactory.cs
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using System.IO;
using System.Linq;

namespace StoreManagementAPI.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Try to set content root - if it fails, let WebApplicationFactory handle it
            try
            {
                var contentRoot = GetProjectPath("StoreManagementAPI", typeof(TProgram).Assembly);
                builder.UseContentRoot(contentRoot);
            }
            catch
            {
                // If we can't find the project path, don't set content root
                // and let WebApplicationFactory try to resolve it automatically
            }

            builder.ConfigureServices(services =>
            {
                // Remove ALL Entity Framework and DbContext related services
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) ||
                    d.ServiceType.Name.Contains("DbContext") ||
                    (d.ImplementationType?.Name.Contains("DbContext") == true) ||
                    (d.ImplementationType?.Name.Contains("SqlServer") == true)).ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Clear and re-add DbContext with only InMemory provider
                services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                {
                    options.UseInMemoryDatabase($"InMemoryDbForTesting_{Guid.NewGuid()}");
                    // Don't use any other providers
                }, ServiceLifetime.Scoped);

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database contexts.
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                    // Ensure the database is created in memory.
                    db.Database.EnsureCreated();

                    // Seed the database with test data
                    if (!db.Companies.Any())
                    {
                        db.Companies.Add(new Company { Id = Guid.Parse("40a0d0a0-e1f2-3456-7890-000000000001"), Name = "Company A" });
                        db.Companies.Add(new Company { Id = Guid.Parse("50a0d0a0-e1f2-3456-7890-000000000002"), Name = "Company B" });
                        db.SaveChanges();
                    }
                    if (!db.Stores.Any())
                    {
                        db.Stores.Add(new Store { Id = Guid.Parse("60a0d0a0-e1f2-3456-7890-000000000001"), Name = "Store 1A", Address = "123 Main St", CompanyId = Guid.Parse("40a0d0d0-e1f2-3456-7890-000000000001") });
                        db.Stores.Add(new Store { Id = Guid.Parse("70a0d0a0-e1f2-3456-7890-000000000002"), Name = "Store 2A", Address = "456 Oak Ave", CompanyId = Guid.Parse("40a0d0d0-e1f2-3456-7890-000000000001") });
                        db.Stores.Add(new Store { Id = Guid.Parse("80a0d0a0-e1f2-3456-7890-000000000003"), Name = "Store 1B", Address = "789 Pine Rd", CompanyId = Guid.Parse("50a0d0d0-e1f2-3456-7890-000000000002") });
                        db.SaveChanges();
                    }
                }
            });
        }

        // Improved helper method to find the content root
        private string GetProjectPath(string projectName, Assembly startupAssembly)
        {
            // Start from the current directory (where tests are running)
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            // Look for the project file starting from current directory and going up
            while (directory != null && !directory.GetFiles($"{projectName}.csproj").Any())
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                // Try alternative approach - look for the project in common relative locations
                var testProjectDir = new DirectoryInfo(currentDirectory);
                var solutionDir = testProjectDir.Parent; // Assuming test project is in a subfolder of solution

                if (solutionDir != null)
                {
                    var projectDirs = solutionDir.GetDirectories(projectName, SearchOption.AllDirectories);
                    var projectDir = projectDirs.FirstOrDefault(d => d.GetFiles($"{projectName}.csproj").Any());

                    if (projectDir != null)
                    {
                        return projectDir.FullName;
                    }
                }

                throw new InvalidOperationException($"Project {projectName}.csproj not found. Unable to set content root.");
            }

            return directory.FullName;
        }
    }
}