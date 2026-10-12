using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class StoreConfiguration : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> entity)
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.ContactName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.Phone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(s => s.Email)
                .HasMaxLength(100);

            entity.Property(s => s.Address)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
