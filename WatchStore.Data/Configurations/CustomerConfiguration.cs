using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Common;
using WatchStore.Core.Entities;
using WatchStore.Data.Context;

namespace WatchStore.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> entity)
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.PhoneNumber)
                .IsRequired()
                .HasMaxLength(PhoneNumber.MaxLength);

            entity.Property(c => c.FullName)
                .HasMaxLength(100);

            entity.Property(c => c.Email)
                .HasMaxLength(200);

            entity.Property(c => c.CustomerType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(c => c.Notes)
                .HasMaxLength(2000);

            entity.HasIndex(c => new { c.BusinessId, c.PhoneNumber })
                .IsUnique()
                .HasDatabaseName(DbConstraintNames.CustomerPhone);

            entity.HasOne(c => c.Business)
                .WithMany()
                .HasForeignKey(c => c.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ConfigureAuditFields();
        }
    }
}
