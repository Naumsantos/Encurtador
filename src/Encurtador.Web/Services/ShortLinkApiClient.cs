using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Encurtador.Web.Models;

namespace Encurtador.Web.Services;

public sealed class ShortLinkApiClient(HttpClient http) : IShortLinkApi
{
    public static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(15);

    public const string UnavailableMessage = "Não foi possível falar com o servidor. Tente novamente.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResult<CreatedLinkDto>> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        var body = new CreateLinkRequest(
            request.DestinationUrl,
            string.IsNullOrWhiteSpace(request.Code) ? null : request.Code,
            request.ExpiresAt);

        try
        {
            using var response = await http.PostAsJsonAsync("api/links", body, JsonOptions, ct);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                return await ReadSuccessAsync<CreatedLinkDto>(response, ct);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return await ReadRejectionAsync<CreatedLinkDto>(response, ct);
            }

            return Unavailable<CreatedLinkDto>();
        }
        catch (Exception exception) when (IsTransportFailure(exception, ct))
        {
            return Unavailable<CreatedLinkDto>();
        }
    }

    public async Task<ApiResult<LinkStatsDto>> GetStatsAsync(string code, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync($"api/links/{Uri.EscapeDataString(code)}/stats", ct);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => await ReadSuccessAsync<LinkStatsDto>(response, ct),
                HttpStatusCode.NotFound => new ApiResult<LinkStatsDto>.NotFound(),
                _ => Unavailable<LinkStatsDto>()
            };
        }
        catch (Exception exception) when (IsTransportFailure(exception, ct))
        {
            return Unavailable<LinkStatsDto>();
        }
    }

    private static async Task<ApiResult<T>> ReadSuccessAsync<T>(HttpResponseMessage response, CancellationToken ct)
        where T : class
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return value is null ? Unavailable<T>() : new ApiResult<T>.Success(value);
    }

    private static async Task<ApiResult<T>> ReadRejectionAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(JsonOptions, ct);
        var detail = problem?.Detail;
        if (string.IsNullOrWhiteSpace(detail))
        {
            return Unavailable<T>();
        }

        return new ApiResult<T>.Rejected(ProblemFieldMapper.Map(detail), detail);
    }

    private static ApiResult<T> Unavailable<T>() => new ApiResult<T>.Unavailable(UnavailableMessage);

    // Cancelamento pedido pelo chamador continua propagando; timeout do HttpClient vira indisponível.
    private static bool IsTransportFailure(Exception exception, CancellationToken ct) =>
        exception is HttpRequestException or JsonException or NotSupportedException
        || (exception is OperationCanceledException && !ct.IsCancellationRequested);

    private sealed record ProblemBody(string? Detail);
}
