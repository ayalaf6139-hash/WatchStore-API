using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> entity)
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.ModelNumber)
                .IsRequired()
                .HasMaxLength(50);

            // אין שני מוצרים עם אותו מספר דגם
            entity.HasIndex(p => p.ModelNumber)
                .IsUnique();

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Brand)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.WholesalePrice)
                .HasPrecision(18, 2);

            entity.Property(p => p.RecommendedRetailPrice)
                .HasPrecision(18, 2);

            // Optimistic concurrency: EF מוסיף את RowVersion לתנאי ה-WHERE
            // בכל UPDATE, ואם השורה השתנתה בינתיים נזרק DbUpdateConcurrencyException
            entity.Property(p => p.RowVersion)
                .IsRowVersion();

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
