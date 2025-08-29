using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public interface IStoreService
    {
        Task<Store> CreateStoreAsync(Store store);
        Task<IEnumerable<Store>> GetAllStoresAsync();
        Task<Store?> GetStoreByIdAsync(Guid id);
        Task<Store?> UpdateStoreAsync(Guid id, Store store);
        Task<bool> DeleteStoreAsync(Guid id);
    }
}