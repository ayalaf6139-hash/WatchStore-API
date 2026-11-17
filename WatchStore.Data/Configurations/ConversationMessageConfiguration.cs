using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;
using WatchStore.Data.Context;

namespace WatchStore.Data.Configurations
{
    public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
    {
        public void Configure(EntityTypeBuilder<ConversationMessage> entity)
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(8000);

            entity.Property(m => m.AgentName)
                .HasMaxLength(50);

            entity.HasIndex(m => new { m.ConversationId, m.SequenceNumber })
                .IsUnique()
                .HasDatabaseName(DbConstraintNames.ConversationMessageSequence);

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ConfigureCreationFields();
        }
    }
}
