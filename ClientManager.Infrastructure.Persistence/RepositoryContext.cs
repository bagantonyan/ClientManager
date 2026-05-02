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
    }
}