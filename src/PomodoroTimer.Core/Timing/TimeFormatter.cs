using System.Globalization;

namespace PomodoroTimer.Core.Timing;

public static class TimeFormatter
{
    /// <summary>
    /// Formats a remaining duration as "mm:ss". Partial seconds are rounded up so a
    /// countdown shows "25:00" until a full second has elapsed and only reaches
    /// "00:00" when the time is actually up.
    /// </summary>
    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "00:00";
        }

        long totalSeconds = (remaining.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
    }
}
