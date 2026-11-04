namespace Encurtador.Application.Links;

public sealed class ShortLink
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string DestinationUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public long AccessCount { get; set; }
}
