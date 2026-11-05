using Encurtador.E2ETests.Infrastructure;
using Microsoft.Playwright;

namespace Encurtador.E2ETests.Links;

[Trait("Category", "E2E")]
[Collection("E2E")]
public sealed class AccessibilityLayoutTests(E2EHostFixture host)
{
    private static readonly ViewportSize Narrow = new() { Width = 320, Height = 640 };

    private static Task<bool> HasHorizontalScrollAsync(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth");

    [Fact]
    public async Task Layout_at_320px_has_no_horizontal_scroll_even_with_long_url()
    {
        var page = await host.NewPageAsync(viewport: Narrow);
        await page.GotoAsync(host.BaseUrl);
        Assert.False(await HasHorizontalScrollAsync(page));

        var longUrl = "https://example.com/" + new string('a', 400);
        await page.GetByTestId("destination-url").FillAsync(longUrl);
        await page.GetByTestId("create-submit").ClickAsync();
        await Assertions.Expect(page.GetByTestId("short-url")).ToBeVisibleAsync();
        Assert.False(await HasHorizontalScrollAsync(page));
    }

    [Fact]
    public async Task Keyboard_only_flow_creates_a_link()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("destination-url").FocusAsync();
        await page.Keyboard.TypeAsync("https://example.com/teclado");
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(page.GetByTestId("short-url")).ToBeVisibleAsync();

        await page.GetByTestId("copy-button").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId("copy-button")).ToHaveTextAsync("Copiado!");
    }

    [Fact]
    public async Task Rejected_submit_moves_focus_to_the_invalid_field_and_links_the_error()
    {
        var page = await host.NewPageAsync();
        await page.GotoAsync(host.BaseUrl);

        await page.GetByTestId("create-submit").ClickAsync();

        var field = page.GetByTestId("destination-url");
        await Assertions.Expect(field).ToBeFocusedAsync();
        await Assertions.Expect(field).ToHaveAttributeAsync("aria-describedby", "destination-url-error");
        await Assertions.Expect(page.Locator("#destination-url-error")).ToBeVisibleAsync();
    }
}
