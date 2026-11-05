using Bunit;
using Encurtador.Web.Components;
using Encurtador.Web.Models;
using Encurtador.Web.Services;
using Encurtador.Web.UnitTests.Infrastructure;
using MudBlazor;

namespace Encurtador.Web.UnitTests.Components;

public sealed class CreateLinkFormErrorTests : BunitTestContext
{
    private void Reject(FormField field, string message) =>
        Api.CreateHandler = _ => Task.FromResult<ApiResult<CreatedLinkDto>>(new ApiResult<CreatedLinkDto>.Rejected(field, message));

    private IRenderedComponent<CreateLinkForm> RenderFilled()
    {
        var cut = Render<CreateLinkForm>();
        CreateLinkFormTests.Fill(cut, "destination-url", "ftp://valor");
        CreateLinkFormTests.Fill(cut, "custom-code", "ab");
        return cut;
    }

    private static void AssertValuesKept(IRenderedComponent<CreateLinkForm> cut)
    {
        Assert.Equal("ftp://valor", cut.Find("[data-testid=destination-url]").GetAttribute("value"));
        Assert.Equal("ab", cut.Find("[data-testid=custom-code]").GetAttribute("value"));
    }

    [Theory]
    [InlineData(FormField.DestinationUrl, ProblemFieldMapper.InvalidUrl, "destination-url")]
    [InlineData(FormField.Code, ProblemFieldMapper.InvalidCode, "custom-code")]
    [InlineData(FormField.Code, ProblemFieldMapper.CodeInUse, "custom-code")]
    public void Api_rejection_is_shown_next_to_the_mapped_field_and_values_are_kept(
        FormField field, string message, string testId)
    {
        Reject(field, message);
        var cut = RenderFilled();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(message, cut.Markup);
            Assert.Equal($"{testId}-error", cut.Find($"[data-testid={testId}]").GetAttribute("aria-describedby"));
        });
        AssertValuesKept(cut);
        Assert.Empty(cut.FindAll("[data-testid=short-url]"));
    }

    [Fact]
    public async Task Past_expiration_is_shown_next_to_the_expiration_field_and_values_are_kept()
    {
        Reject(FormField.Expiration, ProblemFieldMapper.PastExpiration);
        var cut = RenderFilled();
        await CreateLinkFormTests.SetExpirationAsync(cut, new DateTime(2000, 1, 1), new TimeSpan(8, 0, 0));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(ProblemFieldMapper.PastExpiration, cut.Markup);
            Assert.Equal("expiration-date-error", cut.Find("[data-testid=expiration-date]").GetAttribute("aria-describedby"));
        });
        AssertValuesKept(cut);
        Assert.Equal(new DateTime(2000, 1, 1), cut.FindComponent<MudDatePicker>().Instance.Date);
        Assert.Equal(new TimeSpan(8, 0, 0), cut.FindComponent<MudTimePicker>().Instance.Time);
    }

    [Fact]
    public void Unavailable_shows_a_general_error_keeps_values_and_allows_retry()
    {
        var attempts = 0;
        Api.CreateHandler = request => Task.FromResult<ApiResult<CreatedLinkDto>>(++attempts == 1
            ? new ApiResult<CreatedLinkDto>.Unavailable(ShortLinkApiClient.UnavailableMessage)
            : new ApiResult<CreatedLinkDto>.Success(new CreatedLinkDto("abc1234", "http://localhost/abc1234", request.DestinationUrl, DateTimeOffset.UtcNow, null)));
        var cut = RenderFilled();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal(
            "Não foi possível falar com o servidor. Tente novamente.",
            cut.Find("[data-testid=form-error]").TextContent.Trim()));
        AssertValuesKept(cut);
        Assert.False(cut.Find("[data-testid=create-submit]").HasAttribute("disabled"));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("http://localhost/abc1234", cut.Markup));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void General_rejection_is_shown_as_a_general_error_never_on_the_url_field()
    {
        Reject(FormField.General, "O corpo da solicitação contém JSON inválido ou dados obrigatórios ausentes.");
        var cut = RenderFilled();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("JSON inválido", cut.Find("[data-testid=form-error]").TextContent));
        Assert.Null(cut.Find("[data-testid=destination-url]").GetAttribute("aria-describedby"));
        AssertValuesKept(cut);
    }

    [Fact]
    public async Task Time_without_date_shows_local_error_without_calling_the_api()
    {
        var cut = RenderFilled();
        await CreateLinkFormTests.SetExpirationAsync(cut, null, new TimeSpan(10, 0, 0));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Informe também a data de expiração.", cut.Markup));
        Assert.Empty(Api.CreateCalls);
        AssertValuesKept(cut);
    }

    [Fact]
    public void Previous_error_is_cleared_on_the_next_submission()
    {
        Reject(FormField.Code, ProblemFieldMapper.CodeInUse);
        var cut = RenderFilled();
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains(ProblemFieldMapper.CodeInUse, cut.Markup));

        Api.CreateHandler = request => Task.FromResult<ApiResult<CreatedLinkDto>>(new ApiResult<CreatedLinkDto>.Success(
            new CreatedLinkDto("ab", "http://localhost/ab", request.DestinationUrl, DateTimeOffset.UtcNow, null)));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.DoesNotContain(ProblemFieldMapper.CodeInUse, cut.Markup));
    }
}
