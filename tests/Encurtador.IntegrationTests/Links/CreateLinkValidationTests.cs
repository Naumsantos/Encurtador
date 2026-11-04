using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Contracts;
using Encurtador.Application.Links;
using Encurtador.Application.Persistence;
using Encurtador.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.IntegrationTests.Links;

public sealed class CreateLinkValidationTests(LinkApiFixture fixture) : IClassFixture<LinkApiFixture>
{
    [Theory]
    [InlineData("file:///C:/secret", "Valid1")]
    [InlineData("https://example.com", "x")]
    [InlineData("https://example.com", "Invalid!")]
    public async Task Create_returns_portuguese_problem_details_for_invalid_input(string url, string code)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest(url, code, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Solicitação inválida", body);
    }

    [Fact]
    public async Task Create_returns_portuguese_problem_details_for_malformed_json()
    {
        using var request = new StringContent("{ invalid", System.Text.Encoding.UTF8, "application/json");

        var response = await fixture.Client.PostAsync("/api/links", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Solicitação inválida", body);
        Assert.Contains("JSON inválido", body);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_rejects_duplicate_custom_code_without_creating_second_link()
    {
        var request = new CreateLinkRequest("https://example.com/first", "Dup_1", null);
        var firstResponse = await fixture.Client.PostAsJsonAsync("/api/links", request);
        var duplicateResponse = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            request with { DestinationUrl = "https://example.com/second" });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        var statsResponse = await fixture.Client.GetAsync("/api/links/Dup_1/stats");
        var stats = await statsResponse.Content.ReadFromJsonAsync<LinkStatsResponse>();
        Assert.Equal(0, stats?.AccessCount);
    }

    [Fact]
    public async Task Persistence_store_reports_unique_index_conflict_for_concurrent_insert()
    {
        const string code = "Index1";
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com/first", code, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<ILinkStore>();
        var conflict = await store.CreateAsync(new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            DestinationUrl = "https://example.com/second",
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0
        }, default);

        Assert.Equal(LinkCreateStatus.CodeConflict, conflict);
    }
}
