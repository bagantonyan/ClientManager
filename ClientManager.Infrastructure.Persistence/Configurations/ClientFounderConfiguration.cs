using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientManager.Infrastructure.Persistence.Configurations
{
    internal class ClientFounderConfiguration : BaseEntityConfiguration<ClientFounder>
    {
        public override void Configure(EntityTypeBuilder<ClientFounder> builder)
        {
            base.Configure(builder);
        }
    }
}
