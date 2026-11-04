using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.IntegrationTests.Infrastructure;

public sealed class LinkApiFixture : IAsyncLifetime
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkApiFactory _factory;

    public LinkApiFixture()
    {
        _factory = new LinkApiFactory(_database.ConnectionString);
        Client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public HttpClient Client { get; }
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Encurtador.Infrastructure.Persistence.LinksDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        _database.Dispose();
    }
}
