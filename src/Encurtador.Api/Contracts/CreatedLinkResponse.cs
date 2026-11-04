namespace Encurtador.Api.Contracts;

public sealed record CreatedLinkResponse(
    string Code,
    string ShortUrl,
    string DestinationUrl,
    DateTime CreatedAt,
    DateTime? ExpiresAt);
