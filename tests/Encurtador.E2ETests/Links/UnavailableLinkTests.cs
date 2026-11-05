using Encurtador.E2ETests.Infrastructure;
using Microsoft.Playwright;

namespace Encurtador.E2ETests.Links;

[Trait("Category", "E2E")]
[Collection("E2E")]
public sealed class UnavailableLinkTests(E2EHostFixture host)
{
    [Fact]
    public async Task Unknown_code_in_browser_shows_unavailable_page_with_404()
    {
        var page = await host.NewPageAsync();
        var response = await page.GotoAsync($"{host.BaseUrl}/nao{Guid.NewGuid().ToString("N")[..8]}");

        Assert.Equal(404, response!.Status);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Este link não existe ou expirou." }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Expired_link_does_not_redirect_and_offers_a_way_back()
    {
        var code = "old" + Guid.NewGuid().ToString("N")[..6];
        await host.SeedLinkAsync(code, "https://example.com/vencido", DateTime.UtcNow.AddDays(-1), 3);
        var page = await host.NewPageAsync();
        var url = $"{host.BaseUrl}/{code}";

        var response = await page.GotoAsync(url);

        Assert.Equal(404, response!.Status);
        Assert.Equal(url, page.Url);
        await Assertions.Expect(page.GetByText("Este link não existe ou expirou.")).ToBeVisibleAsync();

        await page.GetByTestId("new-link-button").ClickAsync();

        await Assertions.Expect(page.GetByTestId("destination-url")).ToBeVisibleAsync();
        Assert.Equal(host.BaseUrl + "/", page.Url);
    }
}
