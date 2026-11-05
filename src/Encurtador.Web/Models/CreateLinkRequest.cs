using System.Text.Json.Serialization;

namespace Encurtador.Web.Models;

public sealed record CreateLinkRequest(
    string DestinationUrl,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Code = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ExpiresAt = null);
