namespace Encurtador.Application.Links;

public sealed record LinkCreationInput(
    string? DestinationUrl,
    string? Code,
    DateTimeOffset? ExpiresAt);
