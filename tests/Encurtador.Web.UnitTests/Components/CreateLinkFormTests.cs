using Bunit;
using Encurtador.Web.Components;
using Encurtador.Web.Models;
using Encurtador.Web.Services;
using Encurtador.Web.UnitTests.Infrastructure;
using MudBlazor;

namespace Encurtador.Web.UnitTests.Components;

public sealed class CreateLinkFormTests : BunitTestContext
{
    internal static void Fill(IRenderedComponent<CreateLinkForm> cut, string testId, string value) =>
        cut.Find($"[data-testid={testId}]").Input(value);

    internal static async Task SetExpirationAsync(IRenderedComponent<CreateLinkForm> cut, DateTime? date, TimeSpan? time)
    {
        var picker = cut.FindComponent<MudDatePicker>().Instance;
        var timePicker = cut.FindComponent<MudTimePicker>().Instance;
        await cut.InvokeAsync(() => picker.DateChanged.InvokeAsync(date));
        await cut.InvokeAsync(() => timePicker.TimeChanged.InvokeAsync(time));
    }

    [Fact]
    public void Submitting_only_the_url_shows_the_full_short_link()
    {
        var cut = Render<CreateLinkForm>();

        Fill(cut, "destination-url", "https://example.com/pagina");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/abc1234", cut.Find("[data-testid=short-url]").TextContent.Trim()));
        var call = Assert.Single(Api.CreateCalls);
        Assert.Equal("https://example.com/pagina", call.DestinationUrl);
        Assert.Null(call.Code);
        Assert.Null(call.ExpiresAt);
    }

    [Fact]
    public async Task Submitting_with_code_and_future_expiration_sends_both()
    {
        var cut = Render<CreateLinkForm>();

        Fill(cut, "destination-url", "  https://example.com/pagina  ");
        Fill(cut, "custom-code", " meu-link ");
        await SetExpirationAsync(cut, new DateTime(2035, 1, 2), new TimeSpan(8, 30, 0));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Single(Api.CreateCalls));
        var call = Api.CreateCalls[0];
        Assert.Equal("https://example.com/pagina", call.DestinationUrl);
        Assert.Equal("meu-link", call.Code);
        Assert.Equal(new DateTimeOffset(2035, 1, 2, 8, 30, 0, TimeSpan.FromHours(-3)), call.ExpiresAt);
        cut.WaitForAssertion(() => Assert.Contains("http://localhost/meu-link", cut.Markup));
    }

    [Fact]
    public void Copy_button_copies_the_short_url_and_shows_confirmation()
    {
        var cut = Render<CreateLinkForm>();
        Fill(cut, "destination-url", "https://example.com");
        cut.Find("form").Submit();
        cut.WaitForElement("[data-testid=copy-button]");

        cut.Find("[data-testid=copy-button]").Click();

        cut.WaitForAssertion(() => Assert.Contains("Copiado!", cut.Find("[data-testid=copy-button]").TextContent));
        Assert.Equal(["http://localhost/abc1234"], Clipboard.Copied);
    }

    [Fact]
    public void Copy_failure_shows_message_and_keeps_the_link_visible()
    {
        Clipboard.Succeeds = false;
        var cut = Render<CreateLinkForm>();
        Fill(cut, "destination-url", "https://example.com");
        cut.Find("form").Submit();
        cut.WaitForElement("[data-testid=copy-button]");

        cut.Find("[data-testid=copy-button]").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Não foi possível copiar. Selecione o link e copie manualmente.", cut.Markup);
            Assert.Equal("http://localhost/abc1234", cut.Find("[data-testid=short-url]").TextContent.Trim());
        });
    }

    [Fact]
    public async Task Success_clears_every_field_but_keeps_the_link()
    {
        var cut = Render<CreateLinkForm>();
        Fill(cut, "destination-url", "https://example.com");
        Fill(cut, "custom-code", "meu-link");
        await SetExpirationAsync(cut, new DateTime(2035, 1, 2), new TimeSpan(8, 30, 0));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("http://localhost/meu-link", cut.Markup));
        Assert.Equal("", cut.Find("[data-testid=destination-url]").GetAttribute("value") ?? "");
        Assert.Equal("", cut.Find("[data-testid=custom-code]").GetAttribute("value") ?? "");
        Assert.Null(cut.FindComponent<MudDatePicker>().Instance.Date);
        Assert.Null(cut.FindComponent<MudTimePicker>().Instance.Time);
    }

    [Fact]
    public void A_second_link_can_be_created_without_reloading()
    {
        var cut = Render<CreateLinkForm>();
        Fill(cut, "destination-url", "https://example.com/um");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains("http://localhost/abc1234", cut.Markup));

        Fill(cut, "destination-url", "https://example.com/dois");
        Fill(cut, "custom-code", "segundo");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("http://localhost/segundo", cut.Markup));
        Assert.Equal(2, Api.CreateCalls.Count);
    }

    [Fact]
    public void Submit_button_is_disabled_while_sending_and_a_second_submit_is_ignored()
    {
        var gate = new TaskCompletionSource<ApiResult<CreatedLinkDto>>();
        Api.CreateHandler = _ => gate.Task;
        var cut = Render<CreateLinkForm>();
        Fill(cut, "destination-url", "https://example.com");

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.True(cut.Find("[data-testid=create-submit]").HasAttribute("disabled")));
        cut.Find("form").Submit();

        Assert.Single(Api.CreateCalls);
        gate.SetResult(new ApiResult<CreatedLinkDto>.Success(
            new CreatedLinkDto("abc1234", "http://localhost/abc1234", "https://example.com", DateTimeOffset.UtcNow, null)));
        cut.WaitForAssertion(() => Assert.False(cut.Find("[data-testid=create-submit]").HasAttribute("disabled")));
        Assert.Single(Api.CreateCalls);
    }

    [Fact]
    public void Empty_url_shows_local_message_without_calling_the_api()
    {
        var cut = Render<CreateLinkForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Informe a URL de destino.", cut.Markup));
        Assert.Empty(Api.CreateCalls);
    }
}
