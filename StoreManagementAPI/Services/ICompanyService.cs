using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public interface ICompanyService
    {
        Task<Company> CreateCompanyAsync(Company company);
        Task<IEnumerable<Company>> GetAllCompaniesAsync();
        Task<Company?> GetCompanyByIdAsync(Guid id);
        Task<Company?> UpdateCompanyAsync(Guid id, Company company);
        Task<bool> DeleteCompanyAsync(Guid id);
    }
}