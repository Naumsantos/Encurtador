using Microsoft.AspNetCore.Http.HttpResults;

namespace Encurtador.Api.Endpoints;

// Navegadores recebem a página da aplicação web no 404 de GET /{code}; os demais clientes mantêm o ProblemDetails JSON.
public sealed class BrowserNotFoundPageFilter(IWebHostEnvironment environment) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        if (result is not IStatusCodeHttpResult { StatusCode: StatusCodes.Status404NotFound }
            || !AcceptsHtml(context.HttpContext.Request))
        {
            return result;
        }

        var page = environment.WebRootFileProvider.GetFileInfo("index.html");
        return page.Exists ? new HtmlPageResult(page) : result;
    }

    private static bool AcceptsHtml(HttpRequest request) =>
        request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);

    private sealed class HtmlPageResult(Microsoft.Extensions.FileProviders.IFileInfo page) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            httpContext.Response.ContentType = "text/html; charset=utf-8";
            await using var stream = page.CreateReadStream();
            await stream.CopyToAsync(httpContext.Response.Body, httpContext.RequestAborted);
        }
    }
}
