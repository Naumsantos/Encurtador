using Encurtador.Web.Services;

namespace Encurtador.Web.UnitTests.Services;

public sealed class ShortCodeParserTests
{
    [Theory]
    [InlineData("abc1234", "abc1234")]
    [InlineData("  abc1234  ", "abc1234")]
    [InlineData("meu-link_1", "meu-link_1")]
    [InlineData("http://localhost:5086/abc1234", "abc1234")]
    [InlineData("http://localhost:5086/abc1234/", "abc1234")]
    [InlineData("https://exemplo.com/abc1234?x=1", "abc1234")]
    [InlineData("  https://exemplo.com:8443/Abc_12-3  ", "Abc_12-3")]
    public void Extracts_the_code(string input, string expected)
    {
        var result = ShortCodeParser.Parse(input);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://localhost:5086")]
    [InlineData("http://localhost:5086/")]
    [InlineData("código")]
    [InlineData("abc 123")]
    [InlineData("abc/123")]
    [InlineData("ftp://host/abc1234")]
    [InlineData("!!!")]
    public void Rejects_invalid_input_without_throwing(string? input)
    {
        var result = ShortCodeParser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Null(result.Code);
    }
}
