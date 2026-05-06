using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums;

namespace ClientManager.Infrastructure.Persistence.Configurations
{
    internal class ClientConfiguration : BaseEntityConfiguration<Client>
    {
        public override void Configure(EntityTypeBuilder<Client> builder)
        {
            base.Configure(builder);

            builder.HasKey(p => p.Id);

            builder.Property(p => p.INN)
                .IsRequired(true)
                .HasMaxLength(12);

            builder.Property(p => p.Name)
                .IsRequired(true)
                .HasMaxLength(500);

            builder.Property(p => p.ClientType)
                .IsRequired(true)
                .HasConversion<int>();

            builder.HasIndex(p => p.INN)
                .IsUnique()
                .HasFilter("[DeletedDate] IS NULL");

            builder.HasAlternateKey(p => new { p.Id, p.ClientType });

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Client_INN_Format",
                "[INN] NOT LIKE '%[^0-9]%' AND (" +
                $"([ClientType] = {(int)ClientType.Legal_Entity} AND LEN([INN]) = 10) " +
                $"OR ([ClientType] = {(int)ClientType.Individual_Entrepreneur} AND LEN([INN]) = 12))"));
        }
    }
}
