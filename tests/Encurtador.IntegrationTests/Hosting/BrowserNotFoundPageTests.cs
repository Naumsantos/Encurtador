using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Contracts;
using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Encurtador.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encurtador.IntegrationTests.Hosting;

public sealed class BrowserNotFoundPageTests(LinkApiFixture fixture) : IClassFixture<LinkApiFixture>
{
    private const string BrowserAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    private async Task<HttpResponseMessage> GetAsync(string path, string? accept)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (accept is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        return await fixture.Client.SendAsync(request);
    }

    private async Task SeedExpiredAsync(string code)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LinksDbContext>();
        context.Links.Add(new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            DestinationUrl = "https://example.com/old",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();
    }

    private static async Task AssertSpaPageAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<html lang=\"pt-BR\">", html);
        Assert.Contains("blazor.webassembly.js", html);
    }

    [Fact]
    public async Task Unknown_code_for_a_browser_returns_the_spa_page_with_404()
    {
        await AssertSpaPageAsync(await GetAsync("/naoexiste", BrowserAccept));
    }

    [Fact]
    public async Task Expired_code_for_a_browser_returns_the_spa_page_with_404()
    {
        await SeedExpiredAsync("Expir001");

        await AssertSpaPageAsync(await GetAsync("/Expir001", BrowserAccept));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("*/*")]
    [InlineData(null)]
    public async Task Other_clients_keep_the_problem_details_json(string? accept)
    {
        var response = await GetAsync("/naoexiste", accept);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"title\":\"Link não encontrado\"", body);
        Assert.Contains("O código informado não existe ou o link expirou.", body);
    }

    [Fact]
    public async Task Expired_code_for_json_clients_keeps_the_problem_details_json()
    {
        await SeedExpiredAsync("Expir002");

        var response = await GetAsync("/Expir002", "application/json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Valid_link_still_redirects_with_302_for_browsers()
    {
        await fixture.Client.PostAsJsonAsync(
            "/api/links", new CreateLinkRequest("https://example.com/destino", "Valido01", null));

        var response = await GetAsync("/Valido01", BrowserAccept);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(new Uri("https://example.com/destino"), response.Headers.Location);
    }

    [Fact]
    public async Task Root_serves_the_spa_index_page()
    {
        var response = await GetAsync("/", BrowserAccept);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<html lang=\"pt-BR\">", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Store_busy_for_a_browser_is_still_a_json_503()
    {
        using var factory = new LinkApiFactory(
            "Data Source=:memory:",
            services =>
            {
                services.RemoveAll<Encurtador.Application.Persistence.ILinkStore>();
                services.AddSingleton<Encurtador.Application.Persistence.ILinkStore, BusyLinkStore>();
            });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/AnyCode");
        request.Headers.TryAddWithoutValidation("Accept", BrowserAccept);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed class BusyLinkStore : Encurtador.Application.Persistence.ILinkStore
    {
        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<Encurtador.Application.Persistence.LinkCreateStatus> CreateAsync(ShortLink link, CancellationToken cancellationToken) =>
            Task.FromResult(Encurtador.Application.Persistence.LinkCreateStatus.Created);

        public Task<Encurtador.Application.Persistence.LinkAccessResult?> TryRegisterAccessAsync(
            string code, DateTime nowUtc, CancellationToken cancellationToken) =>
            Task.FromException<Encurtador.Application.Persistence.LinkAccessResult?>(
                new Encurtador.Application.Persistence.StoreBusyException(new InvalidOperationException()));

        public Task<LinkStats?> GetStatsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<LinkStats?>(null);
    }
}
