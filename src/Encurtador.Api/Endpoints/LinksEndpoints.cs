using Encurtador.Api.Contracts;
using Encurtador.Application;
using Encurtador.Application.Links;
using Encurtador.Application.Persistence;
using Microsoft.Extensions.Options;

namespace Encurtador.Api.Endpoints;

public static class LinksEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/links", CreateLinkAsync)
            .WithName("CreateLink");
        endpoints.MapGet("/{code}", RedirectAsync)
            .WithName("RedirectToDestination");
        endpoints.MapGet("/api/links/{code}/stats", GetStatsAsync)
            .WithName("GetLinkStats");
        return endpoints;
    }

    private static async Task<IResult> CreateLinkAsync(
        CreateLinkRequest request,
        LinkService linkService,
        IOptions<ShortLinkOptions> options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var link = await linkService.CreateAsync(
                new LinkCreationInput(request.DestinationUrl, request.Code, request.ExpiresAt),
                cancellationToken);
            var shortUrl = new Uri(
                new Uri(options.Value.PublicBaseUrl.TrimEnd('/') + "/", UriKind.Absolute),
                link.Code).AbsoluteUri;
            var response = new CreatedLinkResponse(
                link.Code,
                shortUrl,
                link.DestinationUrl,
                link.CreatedAtUtc,
                link.ExpiresAtUtc);

            return Results.Created(shortUrl, response);
        }
        catch (LinkValidationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitação inválida",
                detail: exception.Message,
                instance: context.Request.Path);
        }
        catch (StoreBusyException)
        {
            return ServiceUnavailable(context);
        }
    }

    private static async Task<IResult> RedirectAsync(
        string code,
        LinkService linkService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var destinationUrl = await linkService.RedirectAsync(code, cancellationToken);
            return destinationUrl is null
                ? NotFound(context)
                : Results.Redirect(destinationUrl, permanent: false, preserveMethod: false);
        }
        catch (StoreBusyException)
        {
            return ServiceUnavailable(context);
        }
    }

    private static async Task<IResult> GetStatsAsync(
        string code,
        LinkService linkService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var stats = await linkService.GetStatsAsync(code, cancellationToken);
            return stats is null
                ? NotFound(context)
                : Results.Ok(new LinkStatsResponse(stats.Code, stats.AccessCount));
        }
        catch (StoreBusyException)
        {
            return ServiceUnavailable(context);
        }
    }

    private static IResult NotFound(HttpContext context)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Link não encontrado",
            detail: "O código informado não existe ou o link expirou.",
            instance: context.Request.Path);
    }

    private static IResult ServiceUnavailable(HttpContext context)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Serviço temporariamente indisponível",
            detail: "Não foi possível registrar o acesso agora. Tente novamente.",
            instance: context.Request.Path);
    }
}
