using Encurtador.Application;
using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceRedirectTests
{
    [Fact]
    public async Task Redirect_returns_destination_and_registers_one_access()
    {
        var store = new FakeLinkStore();
        store.AddExisting(CreateLink("Active1", "https://example.com/destination"));
        var service = new LinkService(store, new ShortCodeGenerator(), TimeProvider.System);

        var destination = await service.RedirectAsync("Active1");

        Assert.Equal("https://example.com/destination", destination);
        Assert.Equal(1, (await store.GetStatsAsync("Active1", default))!.AccessCount);
    }

    [Fact]
    public async Task Redirect_returns_null_and_does_not_count_unknown_code()
    {
        var store = new FakeLinkStore();
        var service = new LinkService(store, new ShortCodeGenerator(), TimeProvider.System);

        var destination = await service.RedirectAsync("Missing");

        Assert.Null(destination);
        Assert.Empty(store.Links);
    }

    [Fact]
    public async Task Redirect_returns_null_and_does_not_count_expired_link()
    {
        var now = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
        var store = new FakeLinkStore();
        store.AddExisting(CreateLink("Expired", "https://example.com", now.UtcDateTime));
        var service = new LinkService(store, new ShortCodeGenerator(), new FixedTimeProvider(now));

        var destination = await service.RedirectAsync("Expired");

        Assert.Null(destination);
        Assert.Equal(0, (await store.GetStatsAsync("Expired", default))!.AccessCount);
    }

    private static ShortLink CreateLink(string code, string destination, DateTime? expiresAt = null)
    {
        return new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            DestinationUrl = destination,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt,
            AccessCount = 0
        };
    }
}
