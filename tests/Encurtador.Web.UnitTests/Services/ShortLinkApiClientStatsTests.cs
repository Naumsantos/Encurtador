using System.Net;
using Encurtador.Web.Models;
using Encurtador.Web.Services;
using Encurtador.Web.UnitTests.Infrastructure;

namespace Encurtador.Web.UnitTests.Services;

public sealed class ShortLinkApiClientStatsTests
{
    private static ShortLinkApiClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5086/") });

    [Fact]
    public async Task Ok_response_is_success_and_escapes_the_code()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"code":"abc1234","accessCount":42}""");

        var result = await CreateClient(handler).GetStatsAsync("abc 1234");

        var success = Assert.IsType<ApiResult<LinkStatsDto>.Success>(result);
        Assert.Equal("abc1234", success.Value.Code);
        Assert.Equal(42, success.Value.AccessCount);
        Assert.Equal("/api/links/abc%201234/stats", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Not_found_response_is_not_found()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.NotFound, "{}", "application/problem+json"));

        Assert.IsType<ApiResult<LinkStatsDto>.NotFound>(await client.GetStatsAsync("zzzz"));
    }

    [Fact]
    public async Task Server_error_is_unavailable()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.InternalServerError, "{}"));

        Assert.IsType<ApiResult<LinkStatsDto>.Unavailable>(await client.GetStatsAsync("abc1234"));
    }

    [Fact]
    public async Task Network_failure_is_unavailable()
    {
        var client = CreateClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("offline")));

        Assert.IsType<ApiResult<LinkStatsDto>.Unavailable>(await client.GetStatsAsync("abc1234"));
    }
}
