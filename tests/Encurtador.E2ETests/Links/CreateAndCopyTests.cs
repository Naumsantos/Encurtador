using System.Globalization;
using Encurtador.E2ETests.Infrastructure;
using Microsoft.Playwright;

namespace Encurtador.E2ETests.Links;

[Trait("Category", "E2E")]
[Collection("E2E")]
public sealed class CreateAndCopyTests(E2EHostFixture host)
{
    private static string Future(int years = 1) =>
        DateTime.Today.AddYears(years).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Creates_a_link_and_shows_the_full_short_url()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("destination-url").FillAsync("https://example.com/pagina-longa");
        await page.GetByTestId("create-submit").ClickAsync();

        var shortUrl = page.GetByTestId("short-url");
        await Assertions.Expect(shortUrl).ToBeVisibleAsync();
        var text = await shortUrl.InnerTextAsync();
        Assert.StartsWith($"{host.BaseUrl}/", text);
        await Assertions.Expect(page.GetByTestId("destination-url")).ToHaveValueAsync("");
    }

    [Fact]
    public async Task Copy_button_puts_the_displayed_link_on_the_clipboard()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("https://example.com/copiar");
        await page.GetByTestId("create-submit").ClickAsync();
        var displayed = await page.GetByTestId("short-url").InnerTextAsync();

        await page.GetByTestId("copy-button").ClickAsync();

        await Assertions.Expect(page.GetByTestId("copy-button")).ToHaveTextAsync("Copiado!");
        var clipboard = await page.EvaluateAsync<string>("navigator.clipboard.readText()");
        Assert.Equal(displayed.Trim(), clipboard);
    }

    [Fact]
    public async Task Creates_a_second_link_without_reloading()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("https://example.com/um");
        await page.GetByTestId("create-submit").ClickAsync();
        await Assertions.Expect(page.GetByTestId("short-url")).ToBeVisibleAsync();
        var first = await page.GetByTestId("short-url").InnerTextAsync();

        await page.GetByTestId("destination-url").FillAsync("https://example.com/dois");
        await page.GetByTestId("create-submit").ClickAsync();

        await Assertions.Expect(page.GetByTestId("short-url")).Not.ToHaveTextAsync(first);
    }

    [Theory]
    [InlineData("Asia/Kolkata", "+05:30")]
    [InlineData("America/Sao_Paulo", "-03:00")]
    public async Task Expiration_is_sent_with_the_browser_time_zone_offset(string timezone, string offset)
    {
        var page = await host.NewPageAsync(timezone);
        await page.GotoAsync(host.BaseUrl);
        await page.GetByTestId("destination-url").FillAsync("https://example.com/expira");
        await page.GetByTestId("expiration-date").FillAsync(Future());
        await page.GetByTestId("expiration-date").PressAsync("Tab");
        await page.GetByTestId("expiration-time").FillAsync("08:30");
        await page.GetByTestId("expiration-time").PressAsync("Tab");

        var request = await page.RunAndWaitForRequestAsync(
            () => page.GetByTestId("create-submit").ClickAsync(),
            r => r.Url.EndsWith("/api/links") && r.Method == "POST");

        var body = request.PostData!;
        var expected = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T08:30:00" + offset;
        Assert.Contains($"\"expiresAt\":\"{expected}\"", body);
        await Assertions.Expect(page.GetByTestId("short-url")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("expires-at")).ToContainTextAsync("08:30");
    }
}
