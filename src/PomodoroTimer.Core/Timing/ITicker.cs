namespace PomodoroTimer.Core.Timing;

/// <summary>
/// A periodic signal that drives the timer. Implementations decide the interval;
/// the timer measures real elapsed time through <see cref="ITimeSource"/>.
/// </summary>
public interface ITicker
{
    event EventHandler? Ticked;

    void Start();

    void Stop();
}
