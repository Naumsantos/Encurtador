using Encurtador.Application.Links;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Infrastructure.Persistence;

public sealed class LinksDbContext(DbContextOptions<LinksDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> Links => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ShortLinkConfiguration());
    }
}
