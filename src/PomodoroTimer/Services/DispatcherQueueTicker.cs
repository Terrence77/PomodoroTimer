using Microsoft.UI.Dispatching;
using PomodoroTimer.Core.Timing;

namespace PomodoroTimer.Services;

/// <summary>
/// <see cref="ITicker"/> backed by a <see cref="DispatcherQueueTimer"/>, so ticks arrive on the
/// UI thread. The interval is shorter than a second to keep the display responsive; the timer
/// itself measures real elapsed time, so the interval does not affect accuracy.
/// </summary>
public sealed class DispatcherQueueTicker : ITicker
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);

    private readonly DispatcherQueueTimer _timer;

    public DispatcherQueueTicker(DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TickInterval;
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => Ticked?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Ticked;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}
