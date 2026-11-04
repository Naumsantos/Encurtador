using Encurtador.Application.Links;
using Encurtador.Application.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Infrastructure.Persistence;

public sealed class EfLinkStore(LinksDbContext dbContext) : ILinkStore
{
    private const int MaximumBusyAttempts = 3;

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return ExecuteWithBusyRetryAsync(
            () => dbContext.Links.AnyAsync(link => link.Code == code, cancellationToken),
            cancellationToken);
    }

    public async Task<LinkCreateStatus> CreateAsync(ShortLink link, CancellationToken cancellationToken)
    {
        dbContext.Links.Add(link);
        try
        {
            await ExecuteWithBusyRetryAsync(
                () => dbContext.SaveChangesAsync(cancellationToken),
                cancellationToken);
            return LinkCreateStatus.Created;
        }
        catch (DbUpdateException exception) when (IsSqliteUniqueConstraint(exception))
        {
            dbContext.Entry(link).State = EntityState.Detached;
            return LinkCreateStatus.CodeConflict;
        }
    }

    public async Task<LinkAccessResult?> TryRegisterAccessAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithBusyRetryAsync(async () =>
        {
            var destinationUrl = await dbContext.Links
                .Where(link => link.Code == code)
                .Select(link => link.DestinationUrl)
                .SingleOrDefaultAsync(cancellationToken);

            if (destinationUrl is null)
            {
                return null;
            }

            var updated = await dbContext.Links
                .Where(link => link.Code == code
                    && (link.ExpiresAtUtc == null || link.ExpiresAtUtc > nowUtc))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(link => link.AccessCount, link => link.AccessCount + 1),
                    cancellationToken);

            if (updated == 0)
            {
                return null;
            }

            return new LinkAccessResult(destinationUrl);
        }, cancellationToken);
    }

    public async Task<LinkStats?> GetStatsAsync(string code, CancellationToken cancellationToken)
    {
        return await ExecuteWithBusyRetryAsync(
            () => dbContext.Links
                .AsNoTracking()
                .Where(link => link.Code == code)
                .Select(link => new LinkStats(link.Code, link.AccessCount))
                .SingleOrDefaultAsync(cancellationToken),
            cancellationToken);
    }

    private static bool IsSqliteUniqueConstraint(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 2067
        };
    }

    private static async Task<T> ExecuteWithBusyRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception exception) when (FindSqliteException(exception) is { } sqliteException
                && IsBusy(sqliteException))
            {
                if (attempt + 1 >= MaximumBusyAttempts)
                {
                    throw new StoreBusyException(sqliteException);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)), cancellationToken);
            }
        }
    }

    private static bool IsBusy(SqliteException exception)
    {
        return exception.SqliteErrorCode is 5 or 6;
    }

    private static SqliteException? FindSqliteException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException sqliteException)
            {
                return sqliteException;
            }
        }

        return null;
    }
}
