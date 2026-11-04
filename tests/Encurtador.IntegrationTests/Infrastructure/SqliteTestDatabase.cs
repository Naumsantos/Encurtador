using Microsoft.Data.Sqlite;

namespace Encurtador.IntegrationTests.Infrastructure;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"encurtador-{Guid.NewGuid():N}.db");

    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = _databasePath,
        Cache = SqliteCacheMode.Shared,
        DefaultTimeout = 2,
        Pooling = false
    }.ToString();

    public void Dispose()
    {
        foreach (var path in new[] { _databasePath, _databasePath + "-shm", _databasePath + "-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
