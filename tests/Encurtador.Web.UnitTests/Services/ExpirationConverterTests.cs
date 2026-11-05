using Encurtador.Web.Services;

namespace Encurtador.Web.UnitTests.Services;

public sealed class ExpirationConverterTests
{
    private static readonly DateTime Date = new(2030, 5, 10);

    [Fact]
    public void Without_date_and_time_expiration_is_omitted()
    {
        var result = ExpirationConverter.Convert(null, null, TimeSpan.FromHours(-3));

        Assert.Null(result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Date_only_uses_default_time_2359()
    {
        var result = ExpirationConverter.Convert(Date, null, TimeSpan.FromHours(-3));

        Assert.Equal("2030-05-10T23:59:00.0000000-03:00", result.Value?.ToString("o"));
    }

    [Fact]
    public void Time_only_is_a_local_error()
    {
        var result = ExpirationConverter.Convert(null, new TimeSpan(10, 0, 0), TimeSpan.Zero);

        Assert.Null(result.Value);
        Assert.Equal("Informe também a data de expiração.", result.Error);
    }

    [Theory]
    [InlineData(-180, "2030-05-10T08:30:00.0000000-03:00")]
    [InlineData(330, "2030-05-10T08:30:00.0000000+05:30")]
    [InlineData(0, "2030-05-10T08:30:00.0000000+00:00")]
    public void Date_and_time_keep_the_browser_offset(int offsetMinutes, string expected)
    {
        var result = ExpirationConverter.Convert(Date, new TimeSpan(8, 30, 0), TimeSpan.FromMinutes(offsetMinutes));

        Assert.Equal(expected, result.Value?.ToString("o"));
    }

    [Fact]
    public void Daylight_saving_change_moves_the_utc_instant()
    {
        var winter = ExpirationConverter.Convert(Date, new TimeSpan(12, 0, 0), TimeSpan.FromHours(-3));
        var summer = ExpirationConverter.Convert(Date, new TimeSpan(12, 0, 0), TimeSpan.FromHours(-2));

        Assert.Equal(TimeSpan.FromHours(1), winter.Value!.Value - summer.Value!.Value);
    }

    [Fact]
    public void Local_date_time_combines_date_and_time()
    {
        Assert.Null(ExpirationConverter.ToLocalDateTime(null, new TimeSpan(8, 0, 0)));
        Assert.Equal(new DateTime(2030, 5, 10, 23, 59, 0), ExpirationConverter.ToLocalDateTime(Date, null));
        Assert.Equal(new DateTime(2030, 5, 10, 8, 15, 0), ExpirationConverter.ToLocalDateTime(Date, new TimeSpan(8, 15, 0)));
    }
}
