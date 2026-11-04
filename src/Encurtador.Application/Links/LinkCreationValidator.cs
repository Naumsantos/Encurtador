namespace Encurtador.Application.Links;

public sealed class LinkCreationValidator
{
    public ValidatedLinkCreation Validate(LinkCreationInput input, DateTime nowUtc)
    {
        var destinationUrl = ValidateDestinationUrl(input.DestinationUrl);
        var expiresAtUtc = input.ExpiresAt?.UtcDateTime;
        if (expiresAtUtc is not null && expiresAtUtc <= nowUtc)
        {
            throw new LinkValidationException("A data de expiração deve ser futura.");
        }

        if (input.Code is not null)
        {
            ValidateCustomCode(input.Code);
        }

        return new ValidatedLinkCreation(destinationUrl, expiresAtUtc);
    }

    private static string ValidateDestinationUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 2048
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new LinkValidationException("Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres.");
        }

        return value;
    }

    private static void ValidateCustomCode(string code)
    {
        if (code.Length is < 4 or > 20 || code.Any(character =>
                !(character is >= 'A' and <= 'Z')
                && !(character is >= 'a' and <= 'z')
                && !(character is >= '0' and <= '9')
                && character is not '-' and not '_'))
        {
            throw new LinkValidationException(
                "O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado.");
        }
    }
}

public sealed record ValidatedLinkCreation(string DestinationUrl, DateTime? ExpiresAtUtc);
