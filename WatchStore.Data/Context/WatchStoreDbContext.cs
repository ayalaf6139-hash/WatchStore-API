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

        public DbSet<Business> Businesses { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<AppointmentType> AppointmentTypes { get; set; }

        public DbSet<AppointmentDurationRule> AppointmentDurationRules { get; set; }

        public DbSet<AppointmentResource> AppointmentResources { get; set; }

        public DbSet<OpeningHours> OpeningHours { get; set; }

        public DbSet<BlockedPeriod> BlockedPeriods { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<Conversation> Conversations { get; set; }

        public DbSet<ConversationMessage> ConversationMessages { get; set; }

        public DbSet<ConversationStateTransition> ConversationStateTransitions { get; set; }

        public DbSet<CustomerRequirement> CustomerRequirements { get; set; }

        public DbSet<AgentAction> AgentActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("btree_gist");

            // טוען את כל מחלקות ה-IEntityTypeConfiguration מתיקיית Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(WatchStoreDbContext).Assembly);

            modelBuilder.Seed();

            modelBuilder.SeedWatchFlow();
        }
    }
}
