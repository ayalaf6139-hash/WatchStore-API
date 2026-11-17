using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class AgentActionConfiguration : IEntityTypeConfiguration<AgentAction>
    {
        public void Configure(EntityTypeBuilder<AgentAction> entity)
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AgentName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.ToolName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.ErrorCode)
                .HasMaxLength(100);

            entity.Property(a => a.Summary)
                .HasMaxLength(500);

            entity.HasIndex(a => new { a.BusinessId, a.CreatedAtUtc });

            entity.HasIndex(a => a.ConversationId);

            entity.HasOne(a => a.Business)
                .WithMany()
                .HasForeignKey(a => a.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Conversation)
                .WithMany()
                .HasForeignKey(a => a.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ConfigureCreationFields();
        }
    }
}
