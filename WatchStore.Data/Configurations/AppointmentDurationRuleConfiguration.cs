using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class AppointmentDurationRuleConfiguration : IEntityTypeConfiguration<AppointmentDurationRule>
    {
        public void Configure(EntityTypeBuilder<AppointmentDurationRule> entity)
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.CustomerType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(r => r.Description)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(r => r.AppointmentType)
                .WithMany(t => t.DurationRules)
                .HasForeignKey(r => r.AppointmentTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ConfigureAuditFields();
        }
    }
}
