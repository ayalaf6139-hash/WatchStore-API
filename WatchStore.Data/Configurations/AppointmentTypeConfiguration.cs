using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;
using WatchStore.Data.Context;

namespace WatchStore.Data.Configurations
{
    public class AppointmentTypeConfiguration : IEntityTypeConfiguration<AppointmentType>
    {
        public void Configure(EntityTypeBuilder<AppointmentType> entity)
        {
            entity.ToTable(t => t.HasCheckConstraint(
                DbConstraintNames.AppointmentTypeDurations,
                "\"BaseDurationMinutes\" > 0 AND \"BufferBeforeMinutes\" >= 0 AND \"BufferAfterMinutes\" >= 0"));

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.Description)
                .HasMaxLength(500);

            entity.HasIndex(t => new { t.BusinessId, t.Name })
                .IsUnique();

            entity.HasOne(t => t.Business)
                .WithMany()
                .HasForeignKey(t => t.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ConfigureAuditFields();
        }
    }
}
