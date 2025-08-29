using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Company entity
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(255);
                entity.HasMany(c => c.Stores)
                      .WithOne(s => s.Company)
                      .HasForeignKey(s => s.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade); // If a company is deleted, its stores are also deleted.
            });

            // Configure Store entity
            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(255);
                entity.Property(s => s.Address).HasMaxLength(500); // Address can be optional
                entity.HasIndex(s => s.CompanyId); // Index for efficient multi-tenant filtering
            });
        }
    }
}
