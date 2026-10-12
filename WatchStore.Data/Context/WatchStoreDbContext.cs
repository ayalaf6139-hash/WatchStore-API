using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Data.Seed;

namespace WatchStore.Data.Context
{
    public class WatchStoreDbContext : DbContext
    {
        public WatchStoreDbContext(
            DbContextOptions<WatchStoreDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Store> Stores { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<ProductTag> ProductTags { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // טוען את כל מחלקות ה-IEntityTypeConfiguration מתיקיית Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(WatchStoreDbContext).Assembly);

            modelBuilder.Seed();
        }
    }
}
