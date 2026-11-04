using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Contracts;
using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Encurtador.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.IntegrationTests.Links;

public sealed class LinkExpirationPersistenceTests
{
    [Fact]
    public async Task Expired_link_is_not_redirected_or_counted_and_records_survive_host_restart()
    {
        using var database = new SqliteTestDatabase();
        await using (var firstFactory = new LinkApiFactory(database.ConnectionString))
        {
            using var client = firstFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            await using (var scope = firstFactory.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LinksDbContext>();
                await context.Database.MigrateAsync();
                context.Links.Add(new ShortLink
                {
                    Id = Guid.NewGuid(),
                    Code = "Expired",
                    DestinationUrl = "https://example.com/old",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                    AccessCount = 5
                });
                await context.SaveChangesAsync();
            }

            var expiredRedirect = await client.GetAsync("/Expired");
            var expiredStats = await client.GetFromJsonAsync<LinkStatsResponse>("/api/links/Expired/stats");
            Assert.Equal(HttpStatusCode.NotFound, expiredRedirect.StatusCode);
            Assert.Equal(5, expiredStats?.AccessCount);

            var createResponse = await client.PostAsJsonAsync(
                "/api/links",
                new CreateLinkRequest("https://example.com/persist", "Persist1", null));
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var redirectResponse = await client.GetAsync("/Persist1");
            Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
        }

        await using (var secondFactory = new LinkApiFactory(database.ConnectionString))
        {
            using var client = secondFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            await using (var scope = secondFactory.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LinksDbContext>();
                await context.Database.MigrateAsync();
            }

            var persistedStats = await client.GetFromJsonAsync<LinkStatsResponse>("/api/links/Persist1/stats");
            var stillExpiredStats = await client.GetFromJsonAsync<LinkStatsResponse>("/api/links/Expired/stats");
            Assert.Equal(1, persistedStats?.AccessCount);
            Assert.Equal(5, stillExpiredStats?.AccessCount);
        }
    }
}
