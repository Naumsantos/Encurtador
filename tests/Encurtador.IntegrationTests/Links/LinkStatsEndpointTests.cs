using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Contracts;
using Encurtador.IntegrationTests.Infrastructure;

namespace Encurtador.IntegrationTests.Links;

public sealed class LinkStatsEndpointTests(LinkApiFixture fixture) : IClassFixture<LinkApiFixture>
{
    [Fact]
    public async Task Stats_are_public_and_do_not_increment_count()
    {
        const string code = "Stats1";
        await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com", code, null));
        await fixture.Client.GetAsync($"/{code}");

        var response = await fixture.Client.GetAsync($"/api/links/{code}/stats");
        var stats = await response.Content.ReadFromJsonAsync<LinkStatsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new LinkStatsResponse(code, 1), stats);
    }

    [Fact]
    public async Task Stats_returns_404_for_unknown_code()
    {
        var response = await fixture.Client.GetAsync("/api/links/Missing/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
