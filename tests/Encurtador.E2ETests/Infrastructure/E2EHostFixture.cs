using System.Net;
using System.Net.Sockets;
using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Playwright;

namespace Encurtador.E2ETests.Infrastructure;

[CollectionDefinition("E2E")]
public sealed class E2ECollection : ICollectionFixture<E2EHostFixture>;

// Sobe a API real (com a interface web) em Kestrel numa porta livre, com SQLite temporário.
public sealed class E2EHostFixture : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"encurtador-e2e-{Guid.NewGuid():N}.db");
    private readonly List<IBrowserContext> _contexts = [];
    private ApiFactory? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public string BaseUrl { get; private set; } = string.Empty;

    private string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = _databasePath,
        DefaultTimeout = 5,
        Pooling = false
    }.ToString();

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        await using (var context = CreateDbContext())
        {
            await context.Database.MigrateAsync();
        }

        _factory = new ApiFactory(ConnectionString, BaseUrl);
        _factory.UseKestrel(port);
        _factory.StartServer();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public async Task<IPage> NewPageAsync(string? timezoneId = "America/Sao_Paulo", ViewportSize? viewport = null)
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "pt-BR",
            TimezoneId = timezoneId,
            Permissions = ["clipboard-read", "clipboard-write"],
            ViewportSize = viewport
        });
        _contexts.Add(context);
        context.SetDefaultTimeout(30_000);
        return await context.NewPageAsync();
    }

    // Grava direto no banco: a API rejeita expiração passada e não permite definir a contagem.
    public async Task SeedLinkAsync(string code, string destinationUrl, DateTime? expiresAtUtc, long accessCount)
    {
        await using var context = CreateDbContext();
        context.Links.Add(new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            DestinationUrl = destinationUrl,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            ExpiresAtUtc = expiresAtUtc,
            AccessCount = accessCount
        });
        await context.SaveChangesAsync();
    }

    private LinksDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<LinksDbContext>().UseSqlite(ConnectionString).Options);

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class ApiFactory(string connectionString, string baseUrl) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Links"] = connectionString,
                    ["Links:PublicBaseUrl"] = baseUrl
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<LinksDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LinksDbContext>>();
                services.AddDbContext<LinksDbContext>(options => options.UseSqlite(connectionString));
            });
        }
    }
}
