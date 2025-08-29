using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StoreManagementAPI.Tests
{
    public class StoresControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly Guid _companyAId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000001");
        private readonly Guid _companyBId = Guid.Parse("c0a0d0a0-e1f2-3456-7890-000000000002");
        private readonly Guid _store1AId = Guid.Parse("s0a0d0a0-e1f2-3456-7890-000000000001");
        private readonly Guid _store1BId = Guid.Parse("s0a0d0a0-e1f2-3456-7890-000000000003");


        public StoresControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_CreateStore_ReturnsCreatedStatusCodeForValidCompany()
        {
            // Arrange
            var newStore = new StoreCreateDto { Name = "New Store A", Address = "100 Test St" };
            var content = new StringContent(JsonConvert.SerializeObject(newStore), Encoding.UTF8, "application/json");

            // Set the X-Company-ID header for the request
            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyAId.ToString());

            // Act
            var response = await _client.PostAsync("/api/Stores", content);

            // Assert
            response.EnsureSuccessStatusCode(); // Checks for 2xx status codes
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var createdStore = JsonConvert.DeserializeObject<Store>(responseString);

            Assert.NotNull(createdStore);
            Assert.NotEqual(Guid.Empty, createdStore.Id);
            Assert.Equal(newStore.Name, createdStore.Name);
            Assert.Equal(_companyAId, createdStore.CompanyId);

            // Verify it's in the in-memory database under the correct company
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var storeInDb = await dbContext.Stores.FirstOrDefaultAsync(s => s.Id == createdStore.Id && s.CompanyId == _companyAId);
                Assert.NotNull(storeInDb);
                Assert.Equal(createdStore.Name, storeInDb.Name);
            }
            _client.DefaultRequestHeaders.Remove("X-Company-ID"); // Clean up header for subsequent tests
        }

        [Fact]
        public async Task Post_CreateStore_ReturnsBadRequestWithoutCompanyIdHeader()
        {
            // Arrange
            var newStore = new StoreCreateDto { Name = "Invalid Store", Address = "Invalid Address" };
            var content = new StringContent(JsonConvert.SerializeObject(newStore), Encoding.UTF8, "application/json");

            // Act - No X-Company-ID header is explicitly added here
            var response = await _client.PostAsync("/api/Stores", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadAsStringAsync();
            Assert.Contains("X-Company-ID header is required.", error);
        }

        [Fact]
        public async Task Get_GetAllStores_ReturnsOnlyStoresForProvidedCompanyId()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyAId.ToString());

            // Act
            var response = await _client.GetAsync("/api/Stores");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var stores = JsonConvert.DeserializeObject<IEnumerable<Store>>(responseString);

            Assert.NotNull(stores);
            Assert.True(stores.Any());
            Assert.Contains(stores, s => s.Id == _store1AId); // Store 1A belongs to Company A, should be returned
            Assert.DoesNotContain(stores, s => s.Id == _store1BId); // Store 1B belongs to Company B, should NOT be returned

            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }

        [Fact]
        public async Task Get_GetStoreById_ReturnsStoreForProvidedCompanyId()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyAId.ToString());

            // Act
            var response = await _client.GetAsync($"/api/Stores/{_store1AId}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var store = JsonConvert.DeserializeObject<Store>(responseString);

            Assert.NotNull(store);
            Assert.Equal(_store1AId, store.Id);
            Assert.Equal(_companyAId, store.CompanyId);

            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }

        [Fact]
        public async Task Get_GetStoreById_ReturnsNotFoundForStoreOfDifferentCompany()
        {
            // Arrange
            // Try to get Store 1A (belongs to Company A) using Company B's ID
            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyBId.ToString());

            // Act
            var response = await _client.GetAsync($"/api/Stores/{_store1AId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }

        [Fact]
        public async Task Put_UpdateStore_ReturnsNoContentForValidCompany()
        {
            // Arrange
            var storeToUpdateId = Guid.Parse("s0a0d0a0-e1f2-3456-7890-000000000002"); // Store 2A for Company A
            var updatedStoreDto = new StoreUpdateDto { Name = "Updated Store 2A", Address = "Updated Address for 2A" };
            var content = new StringContent(JsonConvert.SerializeObject(updatedStoreDto), Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyAId.ToString());

            // Act
            var response = await _client.PutAsync($"/api/Stores/{storeToUpdateId}", content);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify update
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var storeInDb = await dbContext.Stores.FirstOrDefaultAsync(s => s.Id == storeToUpdateId && s.CompanyId == _companyAId);
                Assert.NotNull(storeInDb);
                Assert.Equal("Updated Store 2A", storeInDb.Name);
                Assert.Equal("Updated Address for 2A", storeInDb.Address);
            }
            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }

        [Fact]
        public async Task Put_UpdateStore_ReturnsNotFoundForStoreOfDifferentCompany()
        {
            // Arrange
            // Try to update Store 1A (Company A) with Company B's ID, which should fail
            var storeToUpdateId = _store1AId;
            var updatedStoreDto = new StoreUpdateDto { Name = "Malicious Update", Address = "Malicious Address" };
            var content = new StringContent(JsonConvert.SerializeObject(updatedStoreDto), Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyBId.ToString());

            // Act
            var response = await _client.PutAsync($"/api/Stores/{storeToUpdateId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Verify no changes occurred in the database for the original store
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var originalStore = await dbContext.Stores.FindAsync(storeToUpdateId);
                Assert.NotNull(originalStore);
                Assert.NotEqual("Malicious Update", originalStore.Name); // Ensure it wasn't updated
            }
            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }

        [Fact]
        public async Task Delete_DeleteStore_ReturnsNoContentForValidCompany()
        {
            // Arrange: Create a new store to delete it safely without affecting seeded data for other tests
            var storeToDelete = new StoreCreateDto { Name = "Temp Store for Deletion", Address = "Delete Me St" };
            var createContent = new StringContent(JsonConvert.SerializeObject(storeToDelete), Encoding.UTF8, "application/json");
            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyAId.ToString());
            var createResponse = await _client.PostAsync("/api/Stores", createContent);
            createResponse.EnsureSuccessStatusCode();
            var createdStore = JsonConvert.DeserializeObject<Store>(await createResponse.Content.ReadAsStringAsync());
            Assert.NotNull(createdStore);

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/Stores/{createdStore.Id}");

            // Assert
            deleteResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify deletion
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var storeInDb = await dbContext.Stores.FindAsync(createdStore.Id);
                Assert.Null(storeInDb);
            }
            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }

        [Fact]
        public async Task Delete_DeleteStore_ReturnsNotFoundForStoreOfDifferentCompany()
        {
            // Arrange
            // Try to delete Store 1A (Company A) with Company B's ID
            _client.DefaultRequestHeaders.Add("X-Company-ID", _companyBId.ToString());

            // Act
            var response = await _client.DeleteAsync($"/api/Stores/{_store1AId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Verify no deletion occurred
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var originalStore = await dbContext.Stores.FindAsync(_store1AId);
                Assert.NotNull(originalStore); // The store should still exist
            }
            // Clean up header
            _client.DefaultRequestHeaders.Remove("X-Company-ID");
        }
    }
}
