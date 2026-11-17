using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchStore.Core.Entities;
using WatchStore.Data.Context;

namespace WatchStore.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> entity)
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    DbConstraintNames.AppointmentReservedRange,
                    "\"ReservedStartUtc\" <= \"StartUtc\" AND \"StartUtc\" < \"EndUtc\" AND \"EndUtc\" <= \"ReservedEndUtc\"");

                t.HasCheckConstraint(
                    DbConstraintNames.AppointmentDurationPositive,
                    "\"DurationMinutes\" > 0");
            });

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(a => a.IdempotencyKey)
                .HasMaxLength(100);

            entity.Property(a => a.CustomerNotes)
                .HasMaxLength(1000);

            entity.Property(a => a.PreparationNotes)
                .HasMaxLength(2000);

            entity.Property(a => a.CancellationReason)
                .HasMaxLength(500);

            entity.Property(a => a.RowVersion)
                .IsRowVersion();

            entity.HasIndex(a => new { a.BusinessId, a.StartUtc });

            entity.HasIndex(a => a.CustomerId);

            entity.HasIndex(a => new { a.BusinessId, a.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL")
                .HasDatabaseName(DbConstraintNames.AppointmentIdempotencyKey);

            entity.HasOne(a => a.Business)
                .WithMany()
                .HasForeignKey(a => a.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Customer)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AppointmentType)
                .WithMany()
                .HasForeignKey(a => a.AppointmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AppointmentResource)
                .WithMany()
                .HasForeignKey(a => a.AppointmentResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Conversation)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ConfigureAuditFields();
        }
    }
}
