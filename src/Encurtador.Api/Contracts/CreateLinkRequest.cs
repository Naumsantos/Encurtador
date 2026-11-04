namespace Encurtador.Api.Contracts;

public sealed record CreateLinkRequest(
    string? DestinationUrl,
    string? Code,
    DateTimeOffset? ExpiresAt);
