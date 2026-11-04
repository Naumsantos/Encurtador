using Encurtador.Application.Links;
using Encurtador.Application.Persistence;

namespace Encurtador.UnitTests.Links;

internal sealed class FakeLinkStore : ILinkStore
{
    private readonly Dictionary<string, ShortLink> _links = new(StringComparer.Ordinal);
    private readonly Queue<string> _createConflicts = new();

    public IReadOnlyCollection<ShortLink> Links => _links.Values;

    public void AddExisting(ShortLink link)
    {
        _links.Add(link.Code, link);
    }

    public void ConflictOnNextCreate(string code)
    {
        _createConflicts.Enqueue(code);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(_links.ContainsKey(code));
    }

    public Task<LinkCreateStatus> CreateAsync(ShortLink link, CancellationToken cancellationToken)
    {
        if (_createConflicts.Count > 0 && _createConflicts.Peek() == link.Code)
        {
            _createConflicts.Dequeue();
            return Task.FromResult(LinkCreateStatus.CodeConflict);
        }

        if (_links.ContainsKey(link.Code))
        {
            return Task.FromResult(LinkCreateStatus.CodeConflict);
        }

        _links.Add(link.Code, link);
        return Task.FromResult(LinkCreateStatus.Created);
    }

    public Task<LinkAccessResult?> TryRegisterAccessAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!_links.TryGetValue(code, out var link)
            || (link.ExpiresAtUtc is not null && link.ExpiresAtUtc <= nowUtc))
        {
            return Task.FromResult<LinkAccessResult?>(null);
        }

        link.AccessCount++;
        return Task.FromResult<LinkAccessResult?>(new LinkAccessResult(link.DestinationUrl));
    }

    public Task<LinkStats?> GetStatsAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(_links.TryGetValue(code, out var link)
            ? new LinkStats(link.Code, link.AccessCount)
            : null);
    }
}

internal sealed class SequenceCodeGenerator(params string[] codes) : IShortCodeGenerator
{
    private readonly Queue<string> _codes = new(codes);

    public int Calls { get; private set; }

    public string Generate()
    {
        Calls++;
        return _codes.Count > 0 ? _codes.Dequeue() : throw new InvalidOperationException("No test code configured.");
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
