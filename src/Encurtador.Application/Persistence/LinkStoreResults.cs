using Encurtador.Application.Links;

namespace Encurtador.Application.Persistence;

public enum LinkCreateStatus
{
    Created,
    CodeConflict
}

public sealed record LinkAccessResult(string DestinationUrl);

public sealed class StoreBusyException(Exception innerException)
    : Exception("The persistence provider is temporarily busy.", innerException);
