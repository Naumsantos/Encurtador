using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Encurtador.Infrastructure.Persistence;

namespace Encurtador.IntegrationTests.Infrastructure;

public sealed class LinkApiFactory(
    string connectionString,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Links"] = connectionString,
                ["Links:PublicBaseUrl"] = "http://localhost:5000"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<LinksDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LinksDbContext>>();
            services.AddDbContext<LinksDbContext>(options => options.UseSqlite(connectionString));
            configureServices?.Invoke(services);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LinksDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
