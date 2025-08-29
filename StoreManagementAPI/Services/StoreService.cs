using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantContext _tenantContext;

        public StoreService(ApplicationDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        private IQueryable<Store> GetTenantStores()
        {
            if (!_tenantContext.CompanyId.HasValue)
            {
                throw new UnauthorizedAccessException("Company ID not provided in request.");
            }
            return _context.Stores.Where(s => s.CompanyId == _tenantContext.CompanyId.Value);
        }

        public async Task<Store> CreateStoreAsync(Store store)
        {
            if (!_tenantContext.CompanyId.HasValue)
            {
                throw new UnauthorizedAccessException("Company ID not provided in request.");
            }

            store.Id = Guid.NewGuid();
            store.CompanyId = _tenantContext.CompanyId.Value; // Assign the current tenant's CompanyId

            // Optional: Validate if the CompanyId exists in the Companies table
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == store.CompanyId);
            if (!companyExists)
            {
                throw new InvalidOperationException($"Company with ID {store.CompanyId} does not exist.");
            }

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();
            return store;
        }

        public async Task<IEnumerable<Store>> GetAllStoresAsync()
        {
            return await GetTenantStores().ToListAsync();
        }

        public async Task<Store?> GetStoreByIdAsync(Guid id)
        {
            return await GetTenantStores().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Store?> UpdateStoreAsync(Guid id, Store updatedStore)
        {
            var store = await GetTenantStores().FirstOrDefaultAsync(s => s.Id == id);
            if (store == null)
            {
                return null;
            }

            store.Name = updatedStore.Name;
            store.Address = updatedStore.Address;
            _context.Stores.Update(store);
            await _context.SaveChangesAsync();
            return store;
        }

        public async Task<bool> DeleteStoreAsync(Guid id)
        {
            var store = await GetTenantStores().FirstOrDefaultAsync(s => s.Id == id);
            if (store == null)
            {
                return false;
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}