namespace Encurtador.Web.Models;

public sealed record CreatedLinkDto(
    string Code,
    string ShortUrl,
    string DestinationUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt);
