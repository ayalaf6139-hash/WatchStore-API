using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class ConversationStateTransitionConfiguration : IEntityTypeConfiguration<ConversationStateTransition>
    {
        public void Configure(EntityTypeBuilder<ConversationStateTransition> entity)
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.FromState)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(t => t.ToState)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(t => t.Reason)
                .HasMaxLength(500);

            entity.HasIndex(t => new { t.ConversationId, t.CreatedAtUtc });

            entity.HasOne(t => t.Conversation)
                .WithMany(c => c.StateTransitions)
                .HasForeignKey(t => t.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ConfigureCreationFields();
        }
    }
}
