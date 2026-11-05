using System.Globalization;
using Encurtador.E2ETests.Infrastructure;
using Microsoft.Playwright;

namespace Encurtador.E2ETests.Links;

[Trait("Category", "E2E")]
[Collection("E2E")]
public sealed class RejectionTests(E2EHostFixture host)
{
    [Fact]
    public async Task Invalid_destination_url_shows_error_and_keeps_value()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("ftp://x");
        await page.GetByTestId("create-submit").ClickAsync();

        var error = page.Locator("#destination-url-error");
        await Assertions.Expect(error).ToBeVisibleAsync();
        await Assertions.Expect(error).Not.ToBeEmptyAsync();
        await Assertions.Expect(page.GetByTestId("destination-url")).ToHaveValueAsync("ftp://x");
        await Assertions.Expect(page.GetByTestId("created-link")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Invalid_custom_code_shows_error_and_keeps_values()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("https://example.com/codigo");
        await page.GetByTestId("custom-code").FillAsync("ab");
        await page.GetByTestId("create-submit").ClickAsync();

        var error = page.Locator("#custom-code-error");
        await Assertions.Expect(error).ToBeVisibleAsync();
        await Assertions.Expect(error).Not.ToBeEmptyAsync();
        await Assertions.Expect(page.GetByTestId("custom-code")).ToHaveValueAsync("ab");
        await Assertions.Expect(page.GetByTestId("destination-url")).ToHaveValueAsync("https://example.com/codigo");
    }

    [Fact]
    public async Task Duplicate_custom_code_shows_error_on_the_code_field()
    {
        var code = "dup" + Guid.NewGuid().ToString("N")[..6];
        await host.SeedLinkAsync(code, "https://example.com/existente", null, 0);
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("https://example.com/novo");
        await page.GetByTestId("custom-code").FillAsync(code);
        await page.GetByTestId("create-submit").ClickAsync();

        var error = page.Locator("#custom-code-error");
        await Assertions.Expect(error).ToBeVisibleAsync();
        await Assertions.Expect(error).Not.ToBeEmptyAsync();
        await Assertions.Expect(page.GetByTestId("custom-code")).ToHaveValueAsync(code);
    }

    [Fact]
    public async Task Past_expiration_shows_error_on_the_expiration_field()
    {
        var yesterday = DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("https://example.com/passado");
        await page.GetByTestId("expiration-date").FillAsync(yesterday);
        await page.GetByTestId("expiration-date").PressAsync("Tab");
        await page.GetByTestId("create-submit").ClickAsync();

        var error = page.Locator("#expiration-date-error");
        await Assertions.Expect(error).ToBeVisibleAsync();
        await Assertions.Expect(error).Not.ToBeEmptyAsync();
        await Assertions.Expect(page.GetByTestId("destination-url")).ToHaveValueAsync("https://example.com/passado");
    }
}
