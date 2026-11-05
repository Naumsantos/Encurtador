using Encurtador.E2ETests.Infrastructure;
using Microsoft.Playwright;

namespace Encurtador.E2ETests.Links;

[Trait("Category", "E2E")]
[Collection("E2E")]
public sealed class LookupTests(E2EHostFixture host)
{
    private static string NewCode(string prefix) => prefix + Guid.NewGuid().ToString("N")[..6];

    [Fact]
    public async Task Lookup_by_code_shows_the_access_count()
    {
        var code = NewCode("cnt");
        await host.SeedLinkAsync(code, "https://example.com/contagem", null, 7);
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("lookup-input").FillAsync(code);
        await page.GetByTestId("lookup-submit").ClickAsync();

        await Assertions.Expect(page.GetByTestId("lookup-result")).ToContainTextAsync("7 acessos");
    }

    [Fact]
    public async Task Lookup_by_pasted_short_url_works()
    {
        var code = NewCode("url");
        await host.SeedLinkAsync(code, "https://example.com/colado", null, 1);
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("lookup-input").FillAsync($"{host.BaseUrl}/{code}");
        await page.GetByTestId("lookup-submit").ClickAsync();

        await Assertions.Expect(page.GetByTestId("lookup-result")).ToContainTextAsync("1 acesso");
    }

    [Fact]
    public async Task Lookup_of_unknown_code_shows_not_found_message()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("lookup-input").FillAsync("naoexiste99");
        await page.GetByTestId("lookup-submit").ClickAsync();

        await Assertions.Expect(page.Locator("#lookup-input-error")).ToContainTextAsync("Nenhum link encontrado");
    }

    [Fact]
    public async Task Lookup_of_invalid_input_shows_validation_message()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("lookup-input").FillAsync("a b/c?");
        await page.GetByTestId("lookup-submit").ClickAsync();

        await Assertions.Expect(page.Locator("#lookup-input-error"))
            .ToContainTextAsync("Informe um código ou um link curto válido.");
    }

    [Fact]
    public async Task Lookup_of_expired_link_still_shows_the_total()
    {
        var code = NewCode("exp");
        await host.SeedLinkAsync(code, "https://example.com/expirado", DateTime.UtcNow.AddDays(-1), 12);
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("lookup-input").FillAsync(code);
        await page.GetByTestId("lookup-submit").ClickAsync();

        await Assertions.Expect(page.GetByTestId("lookup-result")).ToContainTextAsync("12 acessos");
    }
}
