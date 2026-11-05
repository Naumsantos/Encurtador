namespace Encurtador.Web.Services;

public interface IClipboardService
{
    // false se negado ou indisponível.
    ValueTask<bool> CopyAsync(string text);
}
