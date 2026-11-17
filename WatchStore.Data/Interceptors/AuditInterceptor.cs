using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WatchStore.Core.Common;
using WatchStore.Core.Interfaces;

namespace WatchStore.Data.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentActor _currentActor;
        private readonly TimeProvider _timeProvider;

        public AuditInterceptor(ICurrentActor currentActor, TimeProvider timeProvider)
        {
            _currentActor = currentActor;
            _timeProvider = timeProvider;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ApplyAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAuditFields(DbContext? context)
        {
            if (context == null)
                return;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var actor = _currentActor.ActorId;

            foreach (var entry in context.ChangeTracker.Entries<ICreationTracked>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAtUtc = now;

                    if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
                        entry.Entity.CreatedBy = actor;
                }
                else if (entry.State == EntityState.Modified && entry.Entity is IAuditable auditable)
                {
                    entry.Property(nameof(ICreationTracked.CreatedAtUtc)).IsModified = false;
                    entry.Property(nameof(ICreationTracked.CreatedBy)).IsModified = false;

                    auditable.UpdatedAtUtc = now;
                    auditable.UpdatedBy = actor;
                }
            }
        }
    }
}
