using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Common;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class BusinessConfiguration : IEntityTypeConfiguration<Business>
    {
        public void Configure(EntityTypeBuilder<Business> entity)
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(b => b.TimeZoneId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(b => b.PhoneNumber)
                .HasMaxLength(PhoneNumber.MaxLength);

            entity.ConfigureAuditFields();
        }
    }
}
