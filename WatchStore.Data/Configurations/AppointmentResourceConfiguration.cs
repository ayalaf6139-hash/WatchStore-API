using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class AppointmentResourceConfiguration : IEntityTypeConfiguration<AppointmentResource>
    {
        public void Configure(EntityTypeBuilder<AppointmentResource> entity)
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(r => new { r.BusinessId, r.Name })
                .IsUnique();

            entity.HasOne(r => r.Business)
                .WithMany()
                .HasForeignKey(r => r.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ConfigureAuditFields();
        }
    }
}
