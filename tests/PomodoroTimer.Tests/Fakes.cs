using PomodoroTimer.Core.Settings;
using PomodoroTimer.Core.Timing;

namespace PomodoroTimer.Tests;

internal sealed class FakeTicker : ITicker
{
    public event EventHandler? Ticked;

    public bool IsStarted { get; private set; }

    public void Start() => IsStarted = true;

    public void Stop() => IsStarted = false;

    public void RaiseTick() => Ticked?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeTimeSource : ITimeSource
{
    public TimeSpan Now { get; set; }

    public void Advance(TimeSpan delta) => Now += delta;
}

internal sealed class InMemorySettingsService : ISettingsService
{
    public PomodoroSettings Stored { get; private set; } = new();

    public int SaveCount { get; private set; }

    public PomodoroSettings Load() => Stored;

    public void Save(PomodoroSettings settings)
    {
        Stored = settings;
        SaveCount++;
    }
}
