using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientManager.Infrastructure.Persistence.Configurations
{
    internal class ClientConfiguration : BaseEntityConfiguration<Client>
    {
        public override void Configure(EntityTypeBuilder<Client> builder)
        {
            base.Configure(builder);

            builder.HasKey(p => p.Id);
        }
    }
}