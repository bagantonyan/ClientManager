using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.Infrastructure.Persistence
{
    public class RepositoryContext : DbContext
    {
        public RepositoryContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Client>? Clients { get; set; }
        public DbSet<Founder>? Founders { get; set; }
        public DbSet<ClientFounder>? ClientFounders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RepositoryContext).Assembly);
        }
    }
}