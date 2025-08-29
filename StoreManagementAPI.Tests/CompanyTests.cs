using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using StoreManagementAPI.Tests;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StoreManagementAPI.Tests
{
    public class CompaniesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public CompaniesControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_CreateCompany_ReturnsCreatedStatusCode()
        {
            // Arrange
            var newCompany = new Company { Name = "Test Company" };
            var content = new StringContent(JsonConvert.SerializeObject(newCompany), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Companies", content);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var createdCompany = JsonConvert.DeserializeObject<Company>(responseString);

            Assert.NotNull(createdCompany);
            Assert.NotEqual(Guid.Empty, createdCompany.Id);
            Assert.Equal(newCompany.Name, createdCompany.Name);

            // Verify it's in the in-memory database
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var companyInDb = await dbContext.Companies.FindAsync(createdCompany.Id);
                Assert.NotNull(companyInDb);
                Assert.Equal(createdCompany.Name, companyInDb.Name);
            }
        }

        [Fact]
        public async Task Get_GetAllCompanies_ReturnsOkStatusCodeAndCompanies()
        {
            // Arrange (companies are seeded in CustomWebApplicationFactory)

            // Act
            var response = await _client.GetAsync("/api/Companies");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var companies = JsonConvert.DeserializeObject<IEnumerable<Company>>(responseString);

            Assert.NotNull(companies);
            Assert.True(companies.Any());
            Assert.Contains(companies, c => c.Name == "Company A");
            Assert.Contains(companies, c => c.Name == "Company B");
        }

        [Fact]
        public async Task Get_GetCompanyById_ReturnsOkStatusCodeAndCompany()
        {
            // Arrange
            var companyId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000001"); // Seeded company

            // Act
            var response = await _client.GetAsync($"/api/Companies/{companyId}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(responseString);

            Assert.NotNull(company);
            Assert.Equal(companyId, company.Id);
            Assert.Equal("Company A", company.Name);
        }

        [Fact]
        public async Task Get_GetCompanyById_ReturnsNotFoundForNonExistentId()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/Companies/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Put_UpdateCompany_ReturnsNoContentStatusCode()
        {
            // Arrange
            var companyId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000001");
            var updatedCompany = new Company { Id = companyId, Name = "Updated Company A" };
            var content = new StringContent(JsonConvert.SerializeObject(updatedCompany), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/Companies/{companyId}", content);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the update in the in-memory database
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var companyInDb = await dbContext.Companies.FindAsync(companyId);
                Assert.NotNull(companyInDb);
                Assert.Equal("Updated Company A", companyInDb.Name);
            }
        }

        [Fact]
        public async Task Delete_DeleteCompany_ReturnsNoContentStatusCode()
        {
            // Arrange
            var companyIdToDelete = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000002"); // Seeded company

            // Act
            var response = await _client.DeleteAsync($"/api/Companies/{companyIdToDelete}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify deletion in the in-memory database
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var companyInDb = await dbContext.Companies.FindAsync(companyIdToDelete);
                Assert.Null(companyInDb);
            }
        }

        [Fact]
        public async Task Delete_DeleteCompany_ReturnsNotFoundForNonExistentId()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/Companies/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}