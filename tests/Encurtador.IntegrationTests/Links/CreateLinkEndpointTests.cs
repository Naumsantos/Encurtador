using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Contracts;
using Encurtador.IntegrationTests.Infrastructure;

namespace Encurtador.IntegrationTests.Links;

public sealed class CreateLinkEndpointTests(LinkApiFixture fixture) : IClassFixture<LinkApiFixture>
{
    [Fact]
    public async Task Create_returns_created_link_and_location_header()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com/docs", "Create1", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost:5000/Create1", response.Headers.Location?.ToString());
        var created = await response.Content.ReadFromJsonAsync<CreatedLinkResponse>();
        Assert.NotNull(created);
        Assert.Equal("Create1", created.Code);
        Assert.Equal("https://example.com/docs", created.DestinationUrl);
    }
}
