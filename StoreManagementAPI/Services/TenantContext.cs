namespace StoreManagementAPI.Services
{
    public class TenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
    }
}
