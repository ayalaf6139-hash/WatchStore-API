using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;
using WatchStore.Data.Context;

namespace WatchStore.Data.Configurations
{
    public class OpeningHoursConfiguration : IEntityTypeConfiguration<OpeningHours>
    {
        public void Configure(EntityTypeBuilder<OpeningHours> entity)
        {
            entity.ToTable(t => t.HasCheckConstraint(
                DbConstraintNames.OpeningHoursRange,
                "\"CloseTime\" > \"OpenTime\""));

            entity.HasKey(h => h.Id);

            entity.HasIndex(h => new { h.BusinessId, h.DayOfWeek });

            entity.HasOne(h => h.Business)
                .WithMany()
                .HasForeignKey(h => h.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ConfigureAuditFields();
        }
    }
}
