using Bunit;
using Encurtador.Web.Components;
using Encurtador.Web.Models;
using Encurtador.Web.Pages;
using Encurtador.Web.Services;
using Encurtador.Web.UnitTests.Infrastructure;

namespace Encurtador.Web.UnitTests.Components;

public sealed class StatsLookupFormTests : BunitTestContext
{
    private void Stats(long count) =>
        Api.StatsHandler = code => Task.FromResult<ApiResult<LinkStatsDto>>(new ApiResult<LinkStatsDto>.Success(new LinkStatsDto(code, count)));

    private static void Lookup(IRenderedComponent<StatsLookupForm> cut, string input)
    {
        cut.Find("[data-testid=lookup-input]").Input(input);
        cut.Find("form[aria-label='Consultar acessos de um link']").Submit();
    }

    [Theory]
    [InlineData(1, "1 acesso")]
    [InlineData(0, "0 acessos")]
    [InlineData(7, "7 acessos")]
    public void Shows_the_total_with_correct_singular_and_plural(long count, string expected)
    {
        Stats(count);
        var cut = Render<StatsLookupForm>();

        Lookup(cut, "abc1234");

        cut.WaitForAssertion(() => Assert.Equal(expected, cut.Find("[data-testid=lookup-result]").TextContent.Trim()));
        Assert.Equal(["abc1234"], Api.StatsCalls);
    }

    [Fact]
    public void Full_short_link_is_reduced_to_its_code()
    {
        Stats(3);
        var cut = Render<StatsLookupForm>();

        Lookup(cut, "  http://localhost:5086/abc1234/  ");

        cut.WaitForAssertion(() => Assert.Contains("3 acessos", cut.Markup));
        Assert.Equal(["abc1234"], Api.StatsCalls);
    }

    [Fact]
    public void Not_found_is_shown_next_to_the_field_and_input_is_kept()
    {
        Api.StatsHandler = _ => Task.FromResult<ApiResult<LinkStatsDto>>(new ApiResult<LinkStatsDto>.NotFound());
        var cut = Render<StatsLookupForm>();

        Lookup(cut, "inexistente");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Nenhum link encontrado para esse código.", cut.Markup);
            Assert.Equal("lookup-input-error", cut.Find("[data-testid=lookup-input]").GetAttribute("aria-describedby"));
        });
        Assert.Equal("inexistente", cut.Find("[data-testid=lookup-input]").GetAttribute("value"));
    }

    [Fact]
    public void Empty_input_asks_for_a_code_without_calling_the_api()
    {
        var cut = Render<StatsLookupForm>();

        cut.Find("form[aria-label='Consultar acessos de um link']").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Informe o código ou o link curto.", cut.Markup));
        Assert.Empty(Api.StatsCalls);
    }

    [Theory]
    [InlineData("não é código")]
    [InlineData("http://localhost:5086/")]
    public void Invalid_text_shows_validation_message_without_calling_the_api(string input)
    {
        var cut = Render<StatsLookupForm>();

        Lookup(cut, input);

        cut.WaitForAssertion(() => Assert.Contains("Informe um código ou um link curto válido.", cut.Markup));
        Assert.Empty(Api.StatsCalls);
        Assert.Equal(input, cut.Find("[data-testid=lookup-input]").GetAttribute("value"));
    }

    [Fact]
    public void Unavailable_shows_a_general_error_in_the_result_area()
    {
        Api.StatsHandler = _ => Task.FromResult<ApiResult<LinkStatsDto>>(
            new ApiResult<LinkStatsDto>.Unavailable(ShortLinkApiClient.UnavailableMessage));
        var cut = Render<StatsLookupForm>();

        Lookup(cut, "abc1234");

        cut.WaitForAssertion(() => Assert.Equal(
            "Não foi possível falar com o servidor. Tente novamente.",
            cut.Find("[data-testid=lookup-result]").TextContent.Trim()));
    }

    [Fact]
    public void Lookup_error_does_not_change_the_creation_form_or_the_displayed_link()
    {
        Api.StatsHandler = _ => Task.FromResult<ApiResult<LinkStatsDto>>(new ApiResult<LinkStatsDto>.NotFound());
        var cut = Render<Home>();
        cut.Find("[data-testid=destination-url]").Input("https://example.com/a");
        cut.Find("form[aria-label='Criar link curto']").Submit();
        cut.WaitForAssertion(() => Assert.Contains("http://localhost/abc1234", cut.Markup));
        cut.Find("[data-testid=destination-url]").Input("https://example.com/rascunho");

        cut.Find("[data-testid=lookup-input]").Input("zzzz");
        cut.Find("form[aria-label='Consultar acessos de um link']").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Nenhum link encontrado para esse código.", cut.Markup));
        Assert.Equal("http://localhost/abc1234", cut.Find("[data-testid=short-url]").TextContent.Trim());
        Assert.Equal("https://example.com/rascunho", cut.Find("[data-testid=destination-url]").GetAttribute("value"));
    }

    [Fact]
    public void Newly_created_link_can_be_looked_up_by_its_short_url()
    {
        Stats(0);
        var cut = Render<Home>();
        cut.Find("[data-testid=destination-url]").Input("https://example.com/a");
        cut.Find("form[aria-label='Criar link curto']").Submit();
        cut.WaitForAssertion(() => Assert.Contains("http://localhost/abc1234", cut.Markup));

        cut.Find("[data-testid=lookup-input]").Input(cut.Find("[data-testid=short-url]").TextContent.Trim());
        cut.Find("form[aria-label='Consultar acessos de um link']").Submit();

        cut.WaitForAssertion(() => Assert.Equal("0 acessos", cut.Find("[data-testid=lookup-result]").TextContent.Trim()));
        Assert.Equal(["abc1234"], Api.StatsCalls);
    }
}
