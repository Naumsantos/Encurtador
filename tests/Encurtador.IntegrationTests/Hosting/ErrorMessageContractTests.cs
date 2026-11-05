using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Encurtador.Api.Contracts;
using Encurtador.IntegrationTests.Infrastructure;

namespace Encurtador.IntegrationTests.Hosting;

// A interface web depende destas mensagens e formatos exatos (research D3/D5).
public sealed class ErrorMessageContractTests(LinkApiFixture fixture) : IClassFixture<LinkApiFixture>
{
    private async Task<string> RejectedDetailAsync(CreateLinkRequest request)
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/links", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("detail").GetString()!;
    }

    [Fact]
    public async Task Invalid_url_message_is_unchanged()
    {
        var detail = await RejectedDetailAsync(new CreateLinkRequest("ftp://example.com", null, null));

        Assert.Equal("Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres.", detail);
    }

    [Fact]
    public async Task Past_expiration_message_is_unchanged()
    {
        var detail = await RejectedDetailAsync(
            new CreateLinkRequest("https://example.com", null, DateTimeOffset.UtcNow.AddDays(-1)));

        Assert.Equal("A data de expiração deve ser futura.", detail);
    }

    [Fact]
    public async Task Invalid_code_message_is_unchanged()
    {
        var detail = await RejectedDetailAsync(new CreateLinkRequest("https://example.com", "ab", null));

        Assert.Equal(
            "O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado.",
            detail);
    }

    [Fact]
    public async Task Duplicate_code_message_is_unchanged()
    {
        var first = await fixture.Client.PostAsJsonAsync(
            "/api/links", new CreateLinkRequest("https://example.com", "contrato-1", null));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var detail = await RejectedDetailAsync(new CreateLinkRequest("https://example.com/2", "contrato-1", null));

        Assert.Equal("O código informado já está em uso.", detail);
    }

    [Fact]
    public async Task Created_response_keeps_the_shape_the_client_reads()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com/forma", "contrato-2", DateTimeOffset.UtcNow.AddDays(1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = body.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(["code", "createdAt", "destinationUrl", "expiresAt", "shortUrl"], names);
        Assert.Equal("contrato-2", body.RootElement.GetProperty("code").GetString());
        Assert.EndsWith("/contrato-2", body.RootElement.GetProperty("shortUrl").GetString());
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Stats_response_keeps_the_shape_the_client_reads()
    {
        await fixture.Client.PostAsJsonAsync(
            "/api/links", new CreateLinkRequest("https://example.com", "contrato-3", null));

        var response = await fixture.Client.GetAsync("/api/links/contrato-3/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = body.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(["accessCount", "code"], names);
    }

    [Fact]
    public async Task Unknown_code_returns_404_problem_details_for_stats_and_redirect()
    {
        foreach (var path in new[] { "/api/links/inexistente/stats", "/inexistente" })
        {
            var response = await fixture.Client.GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Link não encontrado", body.RootElement.GetProperty("title").GetString());
        }
    }
}
