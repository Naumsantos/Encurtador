using Encurtador.Application;
using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceExpirationTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Redirect_counts_only_before_expiration(int secondsToExpiry, bool expectedSuccess)
    {
        var now = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
        var store = new FakeLinkStore();
        store.AddExisting(new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = "Expiry",
            DestinationUrl = "https://example.com",
            CreatedAtUtc = now.AddDays(-1).UtcDateTime,
            ExpiresAtUtc = now.AddSeconds(secondsToExpiry).UtcDateTime,
            AccessCount = 3
        });
        var service = new LinkService(store, new ShortCodeGenerator(), new FixedTimeProvider(now));

        var destination = await service.RedirectAsync("Expiry");

        Assert.Equal(expectedSuccess, destination is not null);
        Assert.Equal(expectedSuccess ? 4 : 3, (await store.GetStatsAsync("Expiry", default))!.AccessCount);
    }
}
