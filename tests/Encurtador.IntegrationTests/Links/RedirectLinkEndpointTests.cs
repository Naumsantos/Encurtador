using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Contracts;
using Encurtador.IntegrationTests.Infrastructure;

namespace Encurtador.IntegrationTests.Links;

public sealed class RedirectLinkEndpointTests(LinkApiFixture fixture) : IClassFixture<LinkApiFixture>
{
    [Fact]
    public async Task Redirect_returns_302_and_counts_each_access()
    {
        const string code = "Redir1";
        await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com/destination", code, null));

        var first = await fixture.Client.GetAsync($"/{code}");
        var second = await fixture.Client.GetAsync($"/{code}");
        var stats = await fixture.Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{code}/stats");

        Assert.Equal(HttpStatusCode.Found, first.StatusCode);
        Assert.Equal(new Uri("https://example.com/destination"), first.Headers.Location);
        Assert.Equal(HttpStatusCode.Found, second.StatusCode);
        Assert.Equal(2, stats?.AccessCount);
    }

    [Fact]
    public async Task Redirect_returns_404_for_unknown_code()
    {
        var response = await fixture.Client.GetAsync("/Unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Concurrent_redirects_do_not_lose_database_increments()
    {
        const string code = "Conc_1";
        await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com/concurrent", code, null));

        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => fixture.Client.GetAsync($"/{code}")));
        var stats = await fixture.Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{code}/stats");

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Found, response.StatusCode));
        Assert.Equal(8, stats?.AccessCount);
    }
}
