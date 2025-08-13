using Microsoft.EntityFrameworkCore;
using Sales.Domain.Entities;

namespace Sales.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core DbContext for the Sales service.
    /// </summary>
    public class SalesDbContext : DbContext
    {
        public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
        {
        }

        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}