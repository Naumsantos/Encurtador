using Microsoft.JSInterop;

namespace Encurtador.Web.Services;

public sealed class BrowserTimeZone(IJSRuntime js) : IBrowserTimeZone
{
    public async ValueTask<TimeSpan> GetOffsetAsync(DateTime localDateTime)
    {
        var minutes = await js.InvokeAsync<double>(
            "encurtador.getOffsetMinutesForLocal",
            localDateTime.Year,
            localDateTime.Month,
            localDateTime.Day,
            localDateTime.Hour,
            localDateTime.Minute);
        return TimeSpan.FromMinutes(minutes);
    }

    public async ValueTask<TimeSpan> GetOffsetAtAsync(DateTimeOffset instant)
    {
        var minutes = await js.InvokeAsync<double>(
            "encurtador.getOffsetMinutesForInstant",
            instant.ToUnixTimeMilliseconds());
        return TimeSpan.FromMinutes(minutes);
    }
}
