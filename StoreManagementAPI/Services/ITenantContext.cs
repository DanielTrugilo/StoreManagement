namespace StoreManagementAPI.Services
{
    public interface ITenantContext
    {
        Guid? CompanyId { get; set; }
    }
}
