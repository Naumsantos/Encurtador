using Microsoft.AspNetCore.Diagnostics;

namespace Encurtador.Api;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException { StatusCode: StatusCodes.Status400BadRequest })
        {
            return false;
        }

        await Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Solicitação inválida",
            detail: "O corpo da solicitação contém JSON inválido ou dados obrigatórios ausentes.",
            instance: httpContext.Request.Path).ExecuteAsync(httpContext);
        return true;
    }
}
