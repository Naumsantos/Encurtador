using System.Net;
using Encurtador.Web.Models;
using Encurtador.Web.Services;
using Encurtador.Web.UnitTests.Infrastructure;

namespace Encurtador.Web.UnitTests.Services;

public sealed class ShortLinkApiClientErrorTests
{
    private static ShortLinkApiClient CreateClient(StubHttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5086/"),
            Timeout = timeout ?? ShortLinkApiClient.ApiTimeout
        });

    private static Task<ApiResult<CreatedLinkDto>> Create(ShortLinkApiClient client) =>
        client.CreateLinkAsync(new CreateLinkRequest("https://example.com"));

    [Theory]
    [InlineData("Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres.", FormField.DestinationUrl)]
    [InlineData("A data de expiração deve ser futura.", FormField.Expiration)]
    [InlineData("O código informado já está em uso.", FormField.Code)]
    [InlineData("O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado.", FormField.Code)]
    [InlineData("O corpo da solicitação contém JSON inválido ou dados obrigatórios ausentes.", FormField.General)]
    public async Task Bad_request_is_rejected_with_the_mapped_field(string detail, FormField field)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { title = "Solicitação inválida", status = 400, detail });
        var client = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.BadRequest, json, "application/problem+json"));

        var result = await Create(client);

        var rejected = Assert.IsType<ApiResult<CreatedLinkDto>.Rejected>(result);
        Assert.Equal(field, rejected.Field);
        Assert.Equal(detail, rejected.Message);
    }

    [Fact]
    public async Task Server_error_is_unavailable_with_portuguese_message()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));

        var result = await Create(client);

        var unavailable = Assert.IsType<ApiResult<CreatedLinkDto>.Unavailable>(result);
        Assert.Equal("Não foi possível falar com o servidor. Tente novamente.", unavailable.Message);
    }

    [Fact]
    public async Task Network_failure_is_unavailable()
    {
        var client = CreateClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("offline")));

        Assert.IsType<ApiResult<CreatedLinkDto>.Unavailable>(await Create(client));
    }

    [Theory]
    [InlineData("não é json")]
    [InlineData("")]
    public async Task Unreadable_json_is_unavailable(string body)
    {
        var created = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.Created, body));
        var rejected = CreateClient(StubHttpMessageHandler.Json(HttpStatusCode.BadRequest, body));

        Assert.IsType<ApiResult<CreatedLinkDto>.Unavailable>(await Create(created));
        Assert.IsType<ApiResult<CreatedLinkDto>.Unavailable>(await Create(rejected));
    }

    [Fact]
    public async Task Request_that_never_completes_becomes_unavailable_after_the_timeout()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler, TimeSpan.FromMilliseconds(200));

        var result = await Create(client);

        Assert.IsType<ApiResult<CreatedLinkDto>.Unavailable>(result);
    }

    [Fact]
    public void Default_timeout_is_fifteen_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(15), ShortLinkApiClient.ApiTimeout);
    }
}
