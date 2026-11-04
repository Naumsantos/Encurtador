using Encurtador.Application.Links;

namespace Encurtador.Application.Persistence;

public interface ILinkStore
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<LinkCreateStatus> CreateAsync(ShortLink link, CancellationToken cancellationToken);

    Task<LinkAccessResult?> TryRegisterAccessAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<LinkStats?> GetStatsAsync(string code, CancellationToken cancellationToken);
}
