using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClientManager.IntegrationTests.Infrastructure
{
    public sealed class RowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Stamp(eventData);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Stamp(eventData);
            return base.SavingChanges(eventData, result);
        }

        private static void Stamp(DbContextEventData eventData)
        {
            if (eventData.Context is null) return;

            var entries = eventData.Context.ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity
                            && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
                ((BaseEntity)entry.Entity).RowVersion = Guid.NewGuid().ToByteArray();
        }
    }
}
