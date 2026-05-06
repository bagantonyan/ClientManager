using ClientManager.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientManager.Infrastructure.Persistence.Configurations
{
    internal class FounderConfiguration : BaseEntityConfiguration<Founder>
    {
        public override void Configure(EntityTypeBuilder<Founder> builder)
        {
            base.Configure(builder);

            builder.HasKey(p => p.Id);

            builder.Property(p => p.INN)
                .IsRequired(true)
                .HasMaxLength(12);

            builder.Property(p => p.FullName)
                .IsRequired(true)
                .HasMaxLength(500);

            builder.HasIndex(p => p.INN)
                .IsUnique()
                .HasFilter("[DeletedDate] IS NULL");

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Founder_INN_Format",
                "LEN([INN]) = 12 AND [INN] NOT LIKE '%[^0-9]%'"));
        }
    }
}
