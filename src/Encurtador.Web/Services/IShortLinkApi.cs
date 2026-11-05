using Encurtador.Web.Models;

namespace Encurtador.Web.Services;

public interface IShortLinkApi
{
    Task<ApiResult<CreatedLinkDto>> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct = default);

    Task<ApiResult<LinkStatsDto>> GetStatsAsync(string code, CancellationToken ct = default);
}
