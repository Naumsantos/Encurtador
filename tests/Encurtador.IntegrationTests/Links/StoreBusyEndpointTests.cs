using System.Net;
using Encurtador.Application.Links;
using Encurtador.Application.Persistence;
using Encurtador.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encurtador.IntegrationTests.Links;

public sealed class StoreBusyEndpointTests
{
    [Fact]
    public async Task Exhausted_store_busy_retries_return_503_problem_details()
    {
        using var factory = new LinkApiFactory(
            "Data Source=:memory:",
            services =>
            {
                services.RemoveAll<ILinkStore>();
                services.AddSingleton<ILinkStore, BusyLinkStore>();
            });
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/AnyCode");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("temporariamente indisponível", body);
    }

    private sealed class BusyLinkStore : ILinkStore
    {
        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<LinkCreateStatus> CreateAsync(ShortLink link, CancellationToken cancellationToken) =>
            Task.FromResult(LinkCreateStatus.Created);

        public Task<LinkAccessResult?> TryRegisterAccessAsync(
            string code,
            DateTime nowUtc,
            CancellationToken cancellationToken) =>
            Task.FromException<LinkAccessResult?>(new StoreBusyException(new InvalidOperationException()));

        public Task<LinkStats?> GetStatsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<LinkStats?>(null);
    }
}
