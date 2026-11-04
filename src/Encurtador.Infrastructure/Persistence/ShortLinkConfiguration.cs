using Encurtador.Application.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encurtador.Infrastructure.Persistence;

public sealed class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public const string CodeIndexName = "IX_Links_Code";

    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("Links");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.Code).HasMaxLength(20).IsRequired().UseCollation("BINARY");
        builder.HasIndex(link => link.Code).IsUnique().HasDatabaseName(CodeIndexName);
        builder.Property(link => link.DestinationUrl).HasMaxLength(2048).IsRequired();
        builder.Property(link => link.CreatedAtUtc).IsRequired();
        builder.Property(link => link.ExpiresAtUtc);
        builder.Property(link => link.AccessCount).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_Links_AccessCount_NonNegative", "AccessCount >= 0"));
    }
}
