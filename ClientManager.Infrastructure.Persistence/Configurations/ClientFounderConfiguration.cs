using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientManager.Infrastructure.Persistence.Configurations
{
    internal class ClientFounderConfiguration : BaseEntityConfiguration<ClientFounder>
    {
        private const string ClientTypeShadow = "ClientType";

        public override void Configure(EntityTypeBuilder<ClientFounder> builder)
        {
            base.Configure(builder);

            builder.HasKey(p => new { p.ClientId, p.FounderId });

            builder.Property<ClientType>(ClientTypeShadow)
                .IsRequired(true);

            builder.HasOne(p => p.Client)
                .WithMany(c => c.ClientFounders)
                .HasForeignKey(nameof(ClientFounder.ClientId), ClientTypeShadow)
                .HasPrincipalKey(c => new { c.Id, c.ClientType })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Founder)
                .WithMany(f => f.ClientFounders)
                .HasForeignKey(p => p.FounderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_ClientFounder_LegalEntityOnly",
                $"[{ClientTypeShadow}] = {(int)ClientType.Legal_Entity}"));
        }
    }
}
