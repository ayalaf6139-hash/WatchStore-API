using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Common;
using WatchStore.Core.Entities;

namespace WatchStore.Data.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> entity)
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.CallerPhoneNumber)
                .IsRequired()
                .HasMaxLength(PhoneNumber.MaxLength);

            entity.Property(c => c.Channel)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(c => c.State)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(c => c.Summary)
                .HasMaxLength(4000);

            entity.Property(c => c.RowVersion)
                .IsRowVersion();

            entity.HasIndex(c => new { c.BusinessId, c.State });

            entity.HasIndex(c => c.CustomerId);

            entity.HasIndex(c => c.CallerPhoneNumber);

            entity.HasOne(c => c.Business)
                .WithMany()
                .HasForeignKey(c => c.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Customer)
                .WithMany(cu => cu.Conversations)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.HandledByUser)
                .WithMany()
                .HasForeignKey(c => c.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ConfigureAuditFields();
        }
    }
}
