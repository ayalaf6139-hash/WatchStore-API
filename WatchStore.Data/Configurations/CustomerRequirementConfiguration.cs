using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class CustomerRequirementConfiguration : IEntityTypeConfiguration<CustomerRequirement>
    {
        public void Configure(EntityTypeBuilder<CustomerRequirement> entity)
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Kind)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(r => r.Certainty)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(r => r.Value)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(r => r.Amount)
                .HasPrecision(18, 2);

            entity.HasIndex(r => r.ConversationId);

            entity.HasIndex(r => r.CustomerId);

            entity.HasOne(r => r.Conversation)
                .WithMany(c => c.Requirements)
                .HasForeignKey(r => r.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.SourceMessage)
                .WithMany()
                .HasForeignKey(r => r.SourceMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ConfigureAuditFields();
        }
    }
}
