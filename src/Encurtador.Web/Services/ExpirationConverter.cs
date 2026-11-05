namespace Encurtador.Web.Services;

public sealed record ExpirationConversion(DateTimeOffset? Value, string? Error);

public static class ExpirationConverter
{
    public const string MissingDateMessage = "Informe também a data de expiração.";

    public static readonly TimeSpan DefaultTime = new(23, 59, 0);

    // Data e hora locais escolhidas pela pessoa; null quando não há data.
    public static DateTime? ToLocalDateTime(DateTime? date, TimeSpan? time) =>
        date is null ? null : date.Value.Date + (time ?? DefaultTime);

    public static ExpirationConversion Convert(DateTime? date, TimeSpan? time, TimeSpan offset)
    {
        if (date is null)
        {
            return time is null
                ? new ExpirationConversion(null, null)
                : new ExpirationConversion(null, MissingDateMessage);
        }

        var local = date.Value.Date + (time ?? DefaultTime);
        return new ExpirationConversion(new DateTimeOffset(local, offset), null);
    }
}
