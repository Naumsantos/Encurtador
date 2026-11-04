namespace Encurtador.Application.Links;

public sealed record CreatedLink(
    string Code,
    string DestinationUrl,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc);
