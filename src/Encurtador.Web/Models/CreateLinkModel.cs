namespace Encurtador.Web.Models;

public sealed class CreateLinkModel
{
    public string? DestinationUrl { get; set; }
    public string? Code { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public TimeSpan? ExpirationTime { get; set; }

    public string TrimmedDestinationUrl => DestinationUrl?.Trim() ?? string.Empty;

    // Código vazio ou só espaços é tratado como não informado.
    public string? TrimmedCode => string.IsNullOrWhiteSpace(Code) ? null : Code.Trim();

    public void Clear()
    {
        DestinationUrl = null;
        Code = null;
        ExpirationDate = null;
        ExpirationTime = null;
    }
}
