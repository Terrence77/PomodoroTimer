using PomodoroTimer.Core.Timing;

namespace PomodoroTimer.Tests;

public sealed class TimeFormatterTests
{
    [Theory]
    [InlineData(25 * 60, "25:00")]
    [InlineData(5, "00:05")]
    [InlineData(0, "00:00")]
    [InlineData(59, "00:59")]
    [InlineData(60, "01:00")]
    [InlineData(5 * 60, "05:00")]
    [InlineData(120 * 60, "120:00")]
    public void FormatRemaining_WholeSeconds(int seconds, string expected)
    {
        Assert.Equal(expected, TimeFormatter.FormatRemaining(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void FormatRemaining_RoundsPartialSecondsUp()
    {
        Assert.Equal("25:00", TimeFormatter.FormatRemaining(TimeSpan.FromMinutes(25) - TimeSpan.FromMilliseconds(300)));
        Assert.Equal("00:05", TimeFormatter.FormatRemaining(TimeSpan.FromMilliseconds(4200)));
        Assert.Equal("00:01", TimeFormatter.FormatRemaining(TimeSpan.FromTicks(1)));
    }

    [Fact]
    public void FormatRemaining_NegativeIsZero()
    {
        Assert.Equal("00:00", TimeFormatter.FormatRemaining(TimeSpan.FromSeconds(-3)));
    }
}
