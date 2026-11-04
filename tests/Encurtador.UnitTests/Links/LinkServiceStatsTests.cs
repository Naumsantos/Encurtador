using Encurtador.Application;
using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceStatsTests
{
    [Fact]
    public async Task Stats_returns_accumulated_count_without_mutating_it()
    {
        var store = new FakeLinkStore();
        store.AddExisting(new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = "Stats1",
            DestinationUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 12
        });
        var service = new LinkService(store, new ShortCodeGenerator(), TimeProvider.System);

        var stats = await service.GetStatsAsync("Stats1");

        Assert.Equal(new LinkStats("Stats1", 12), stats);
        Assert.Equal(12, (await store.GetStatsAsync("Stats1", default))!.AccessCount);
    }

    [Fact]
    public async Task Stats_are_available_for_expired_links()
    {
        var store = new FakeLinkStore();
        store.AddExisting(new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = "OldLink",
            DestinationUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            AccessCount = 5
        });
        var service = new LinkService(store, new ShortCodeGenerator(), TimeProvider.System);

        var stats = await service.GetStatsAsync("OldLink");

        Assert.Equal(new LinkStats("OldLink", 5), stats);
    }

    [Fact]
    public async Task Stats_returns_null_for_unknown_code()
    {
        var service = new LinkService(new FakeLinkStore(), new ShortCodeGenerator(), TimeProvider.System);

        Assert.Null(await service.GetStatsAsync("Missing"));
    }
}
