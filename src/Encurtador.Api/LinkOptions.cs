namespace Encurtador.Api;

public sealed class ShortLinkOptions
{
    public const string SectionName = "Links";

    public string PublicBaseUrl { get; set; } = string.Empty;
}
