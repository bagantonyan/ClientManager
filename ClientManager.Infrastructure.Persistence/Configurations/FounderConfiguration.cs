using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientManager.Infrastructure.Persistence.Configurations
{
    internal class FounderConfiguration : BaseEntityConfiguration<Founder>
    {
        public override void Configure(EntityTypeBuilder<Founder> builder)
        {
            base.Configure(builder);

            builder.HasKey(p => p.Id);
        }
    }
}