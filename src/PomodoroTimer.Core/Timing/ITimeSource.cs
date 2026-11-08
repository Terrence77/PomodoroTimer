namespace PomodoroTimer.Core.Timing;

/// <summary>A monotonic clock.</summary>
public interface ITimeSource
{
    /// <summary>Gets a monotonically increasing timestamp.</summary>
    TimeSpan Now { get; }
}
