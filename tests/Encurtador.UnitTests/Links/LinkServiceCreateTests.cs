using Encurtador.Application;
using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceCreateTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("http://example.com/a")]
    [InlineData("https://example.com/a")]
    public async Task Create_accepts_absolute_http_and_https_urls(string url)
    {
        var store = new FakeLinkStore();
        var service = CreateService(store);

        var result = await service.CreateAsync(new LinkCreationInput(url, "Code_1", null));

        Assert.Equal(url, result.DestinationUrl);
        Assert.Equal("Code_1", result.Code);
        Assert.Single(store.Links);
    }

    [Fact]
    public async Task Create_accepts_url_with_exactly_2048_characters()
    {
        var prefix = "https://example.com/";
        var url = prefix + new string('a', 2048 - prefix.Length);
        var result = await CreateService(new FakeLinkStore())
            .CreateAsync(new LinkCreationInput(url, "Code_1", null));

        Assert.Equal(2048, result.DestinationUrl.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("file:///C:/secret")]
    [InlineData("ftp://example.com/file")]
    [InlineData("http:///missing-host")]
    public async Task Create_rejects_invalid_destination_urls(string? url)
    {
        var store = new FakeLinkStore();

        var exception = await Assert.ThrowsAsync<LinkValidationException>(
            () => CreateService(store).CreateAsync(new LinkCreationInput(url, "Code_1", null)));

        Assert.Contains("URL", exception.Message);
        Assert.Empty(store.Links);
    }

    [Fact]
    public async Task Create_rejects_destination_longer_than_2048_characters()
    {
        var prefix = "https://example.com/";
        var url = prefix + new string('a', 2049 - prefix.Length);
        var store = new FakeLinkStore();

        await Assert.ThrowsAsync<LinkValidationException>(
            () => CreateService(store).CreateAsync(new LinkCreationInput(url, "Code_1", null)));

        Assert.Empty(store.Links);
    }

    [Theory]
    [InlineData("Ab1")]
    [InlineData("A12345678901234567890")]
    [InlineData("a!bc")]
    [InlineData("éabc")]
    [InlineData("a bc")]
    public async Task Create_rejects_invalid_custom_codes(string code)
    {
        var store = new FakeLinkStore();

        var exception = await Assert.ThrowsAsync<LinkValidationException>(
            () => CreateService(store).CreateAsync(new LinkCreationInput("https://example.com", code, null)));

        Assert.Contains("código", exception.Message);
        Assert.Empty(store.Links);
    }

    [Fact]
    public async Task Create_accepts_custom_code_with_20_allowed_characters()
    {
        const string code = "Abcdefghijklmnopqrs_";
        var result = await CreateService(new FakeLinkStore())
            .CreateAsync(new LinkCreationInput("https://example.com", code, null));

        Assert.Equal(20, result.Code.Length);
    }

    [Fact]
    public async Task Create_normalizes_expiration_to_utc_and_persists_it()
    {
        var expiry = Now.AddHours(3).ToOffset(TimeSpan.FromHours(2));
        var result = await CreateService(new FakeLinkStore())
            .CreateAsync(new LinkCreationInput("https://example.com", "Code_1", expiry));

        Assert.Equal(expiry.UtcDateTime, result.ExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, result.ExpiresAtUtc!.Value.Kind);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Create_rejects_expiration_at_or_before_creation(int secondsFromNow)
    {
        var store = new FakeLinkStore();
        var expiry = Now.AddSeconds(secondsFromNow);

        var exception = await Assert.ThrowsAsync<LinkValidationException>(
            () => CreateService(store).CreateAsync(new LinkCreationInput("https://example.com", "Code_1", expiry)));

        Assert.Contains("expiração", exception.Message);
        Assert.Empty(store.Links);
    }

    [Fact]
    public async Task Create_rejects_custom_code_already_in_use_without_partial_link()
    {
        var store = new FakeLinkStore();
        store.AddExisting(new ShortLink { Id = Guid.NewGuid(), Code = "Code_1", DestinationUrl = "https://old.example" });

        await Assert.ThrowsAsync<LinkValidationException>(
            () => CreateService(store).CreateAsync(new LinkCreationInput("https://example.com", "Code_1", null)));

        Assert.Single(store.Links);
    }

    [Fact]
    public async Task Create_rejects_unique_index_race_for_custom_code()
    {
        var store = new FakeLinkStore();
        store.ConflictOnNextCreate("Code_1");

        await Assert.ThrowsAsync<LinkValidationException>(
            () => CreateService(store).CreateAsync(new LinkCreationInput("https://example.com", "Code_1", null)));

        Assert.Empty(store.Links);
    }

    private static LinkService CreateService(FakeLinkStore store)
    {
        return new LinkService(store, new ShortCodeGenerator(), new FixedTimeProvider(Now));
    }
}
