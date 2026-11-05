using Bunit;
using Encurtador.Web.Pages;
using Encurtador.Web.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.Web.UnitTests.Components;

public sealed class LinkUnavailableTests : BunitTestContext
{
    [Fact]
    public void Shows_the_unavailable_message_and_the_new_link_button()
    {
        var cut = Render<LinkUnavailable>(parameters => parameters.Add(p => p.Path, "abc1234"));

        Assert.Equal("Este link não existe ou expirou.", cut.Find("h1").TextContent.Trim());
        Assert.Equal("Criar um novo link", cut.Find("[data-testid=new-link-button]").TextContent.Trim());
    }

    [Fact]
    public void Button_navigates_to_the_home_page()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/abc1234");
        var cut = Render<LinkUnavailable>(parameters => parameters.Add(p => p.Path, "abc1234"));

        cut.Find("[data-testid=new-link-button]").Click();

        Assert.Equal(navigation.BaseUri, navigation.Uri);
    }
}
