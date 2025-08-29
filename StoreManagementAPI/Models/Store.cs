namespace StoreManagementAPI.Models
{
    public class Store
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Foreign Key for multi-tenancy
        public Guid CompanyId { get; set; }

        // Navigation property for the parent company
        public Company? Company { get; set; }
    }

    // DTO for creating a new store, without the Id
    public class StoreCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    // DTO for updating an existing store
    public class StoreUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}