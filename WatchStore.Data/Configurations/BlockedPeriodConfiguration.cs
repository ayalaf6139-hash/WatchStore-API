using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;
using WatchStore.Data.Context;

namespace WatchStore.Data.Configurations
{
    public class BlockedPeriodConfiguration : IEntityTypeConfiguration<BlockedPeriod>
    {
        public void Configure(EntityTypeBuilder<BlockedPeriod> entity)
        {
            entity.ToTable(t => t.HasCheckConstraint(
                DbConstraintNames.BlockedPeriodRange,
                "\"EndUtc\" > \"StartUtc\""));

            entity.HasKey(b => b.Id);

            entity.Property(b => b.Reason)
                .HasMaxLength(200);

            entity.HasIndex(b => new { b.BusinessId, b.StartUtc });

            entity.HasOne(b => b.Business)
                .WithMany()
                .HasForeignKey(b => b.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.AppointmentResource)
                .WithMany()
                .HasForeignKey(b => b.AppointmentResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ConfigureAuditFields();
        }
    }
}
