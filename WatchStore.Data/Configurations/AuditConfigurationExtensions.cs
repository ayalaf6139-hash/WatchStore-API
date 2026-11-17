using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Common;

namespace WatchStore.Data.Configurations
{
    public static class AuditConfigurationExtensions
    {
        public const int ActorMaxLength = 100;

        public static void ConfigureCreationFields<T>(this EntityTypeBuilder<T> entity)
            where T : class, ICreationTracked
        {
            entity.Property(nameof(ICreationTracked.CreatedAtUtc))
                .IsRequired();

            entity.Property(nameof(ICreationTracked.CreatedBy))
                .IsRequired()
                .HasMaxLength(ActorMaxLength);
        }

        public static void ConfigureAuditFields<T>(this EntityTypeBuilder<T> entity)
            where T : class, IAuditable
        {
            entity.ConfigureCreationFields();

            entity.Property(nameof(IAuditable.UpdatedBy))
                .HasMaxLength(ActorMaxLength);
        }
    }
}
