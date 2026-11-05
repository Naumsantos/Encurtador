using Microsoft.JSInterop;

namespace Encurtador.Web.Services;

public sealed class ClipboardService(IJSRuntime js) : IClipboardService
{
    public async ValueTask<bool> CopyAsync(string text)
    {
        try
        {
            return await js.InvokeAsync<bool>("encurtador.copyText", text);
        }
        catch (JSException)
        {
            return false;
        }
    }
}
