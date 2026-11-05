using System.Net;
using System.Text.Json;
using Encurtador.Web.Models;
using Encurtador.Web.Services;
using Encurtador.Web.UnitTests.Infrastructure;

namespace Encurtador.Web.UnitTests.Services;

public sealed class ShortLinkApiClientCreateTests
{
    private const string CreatedJson = """
        {"code":"abc1234","shortUrl":"http://localhost:5086/abc1234","destinationUrl":"https://example.com/x",
         "createdAt":"2030-01-01T10:00:00+00:00","expiresAt":"2030-02-01T02:59:00+00:00"}
        """;

    private static ShortLinkApiClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5086/") });

    [Fact]
    public async Task Created_response_maps_all_fields()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.Created, CreatedJson));

        var result = await client.CreateLinkAsync(new CreateLinkRequest("https://example.com/x"));

        var success = Assert.IsType<ApiResult<CreatedLinkDto>.Success>(result);
        Assert.Equal("abc1234", success.Value.Code);
        Assert.Equal("http://localhost:5086/abc1234", success.Value.ShortUrl);
        Assert.Equal("https://example.com/x", success.Value.DestinationUrl);
        Assert.Equal(DateTimeOffset.Parse("2030-01-01T10:00:00+00:00"), success.Value.CreatedAt);
        Assert.Equal(DateTimeOffset.Parse("2030-02-01T02:59:00+00:00"), success.Value.ExpiresAt);
    }

    [Fact]
    public async Task Request_omits_empty_code_and_missing_expiration()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.Created, CreatedJson);

        await CreateClient(handler).CreateLinkAsync(new CreateLinkRequest("https://example.com/x", "  "));

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("https://example.com/x", body.RootElement.GetProperty("destinationUrl").GetString());
        Assert.False(body.RootElement.TryGetProperty("code", out _));
        Assert.False(body.RootElement.TryGetProperty("expiresAt", out _));
        Assert.Equal("/api/links", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Request_sends_expiration_in_iso_8601_with_offset()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.Created, CreatedJson);
        var expiresAt = new DateTimeOffset(2030, 2, 1, 23, 59, 0, TimeSpan.FromHours(-3));

        await CreateClient(handler).CreateLinkAsync(new CreateLinkRequest("https://example.com/x", "meu-link", expiresAt));

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("meu-link", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("2030-02-01T23:59:00-03:00", body.RootElement.GetProperty("expiresAt").GetString());
    }
}
