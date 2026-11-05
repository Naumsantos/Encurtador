namespace Encurtador.Web.Services;

public sealed record ShortCodeParseResult(bool IsValid, string? Code)
{
    public static readonly ShortCodeParseResult Invalid = new(false, null);
}

public static class ShortCodeParser
{
    public static ShortCodeParseResult Parse(string? input)
    {
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return ShortCodeParseResult.Invalid;
        }

        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return ShortCodeParseResult.Invalid;
            }

            text = Uri.UnescapeDataString(segments[^1]);
        }

        return text.All(IsCodeCharacter) ? new ShortCodeParseResult(true, text) : ShortCodeParseResult.Invalid;
    }

    private static bool IsCodeCharacter(char c) =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_';
}
