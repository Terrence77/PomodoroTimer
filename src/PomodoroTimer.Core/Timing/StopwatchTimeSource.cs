using System.Diagnostics;

namespace PomodoroTimer.Core.Timing;

/// <summary>Monotonic clock backed by <see cref="Stopwatch"/>.</summary>
public sealed class StopwatchTimeSource : ITimeSource
{
    private readonly long _origin = Stopwatch.GetTimestamp();

    public TimeSpan Now => Stopwatch.GetElapsedTime(_origin);
}
