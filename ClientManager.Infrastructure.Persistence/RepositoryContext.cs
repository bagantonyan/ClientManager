using ClientManager.Core.Domain.Entities;
using ClientManager.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.Infrastructure.Persistence
{
    public class RepositoryContext : DbContext
    {
        private readonly TimeProvider _timeProvider;

        public RepositoryContext(DbContextOptions<RepositoryContext> options, TimeProvider timeProvider)
            : base(options)
        {
            _timeProvider = timeProvider;
        }

        public DbSet<Client>? Clients { get; set; }
        public DbSet<Founder>? Founders { get; set; }
        public DbSet<ClientFounder>? ClientFounders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RepositoryContext).Assembly);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.SetAuditProperties(_timeProvider);

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}