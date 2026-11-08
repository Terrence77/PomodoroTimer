namespace PomodoroTimer.Core.Timing;

/// <summary>
/// Pomodoro countdown state machine. Alternates between <see cref="TimerPhase.Work"/>
/// and <see cref="TimerPhase.Break"/> automatically and keeps running across phase switches.
/// </summary>
public sealed class PomodoroTimerService
{
    private readonly ITicker _ticker;
    private readonly ITimeSource _timeSource;
    private TimeSpan _lastTimestamp;

    public PomodoroTimerService(ITicker ticker, ITimeSource timeSource, TimeSpan workDuration, TimeSpan breakDuration)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(timeSource);
        ValidateDuration(workDuration);
        ValidateDuration(breakDuration);

        _ticker = ticker;
        _timeSource = timeSource;
        WorkDuration = workDuration;
        BreakDuration = breakDuration;
        CurrentPhaseDuration = workDuration;
        Remaining = workDuration;

        _ticker.Ticked += OnTicked;
    }

    /// <summary>Raised whenever <see cref="Phase"/>, <see cref="Remaining"/> or <see cref="IsRunning"/> changes.</summary>
    public event EventHandler? StateChanged;

    public TimerPhase Phase { get; private set; } = TimerPhase.Work;

    public TimeSpan Remaining { get; private set; }

    public bool IsRunning { get; private set; }

    public TimeSpan WorkDuration { get; private set; }

    public TimeSpan BreakDuration { get; private set; }

    /// <summary>The full length of the phase currently counting down.</summary>
    public TimeSpan CurrentPhaseDuration { get; private set; }

    /// <summary>True when the timer is stopped at the very beginning of the current phase.</summary>
    public bool IsAtPhaseStart => !IsRunning && Remaining == CurrentPhaseDuration;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _lastTimestamp = _timeSource.Now;
        IsRunning = true;
        _ticker.Start();
        OnStateChanged();
    }

    public void Pause()
    {
        if (!IsRunning)
        {
            return;
        }

        // Account for the time elapsed since the last tick before stopping.
        AdvanceToNow();
        _ticker.Stop();
        IsRunning = false;
        OnStateChanged();
    }

    /// <summary>Stops the timer and returns to the start of a work phase using the current durations.</summary>
    public void Reset()
    {
        _ticker.Stop();
        IsRunning = false;
        LoadPhase(TimerPhase.Work);
        OnStateChanged();
    }

    /// <summary>
    /// Updates the phase durations. A countdown in progress is not interrupted: new values
    /// take effect at the next phase switch or on <see cref="Reset"/>. If the timer is idle
    /// at the very start of a phase, that phase is reloaded right away.
    /// </summary>
    public void SetDurations(TimeSpan workDuration, TimeSpan breakDuration)
    {
        ValidateDuration(workDuration);
        ValidateDuration(breakDuration);

        bool reloadCurrentPhase = IsAtPhaseStart;
        WorkDuration = workDuration;
        BreakDuration = breakDuration;

        if (reloadCurrentPhase)
        {
            LoadPhase(Phase);
            OnStateChanged();
        }
    }

    /// <summary>
    /// Advances the countdown by <paramref name="elapsed"/>. When the remaining time reaches
    /// zero the timer switches phase, loads that phase's duration, and keeps running.
    /// Ignored while paused.
    /// </summary>
    public void Tick(TimeSpan elapsed)
    {
        if (!IsRunning || elapsed <= TimeSpan.Zero)
        {
            return;
        }

        Remaining -= elapsed;
        while (Remaining <= TimeSpan.Zero)
        {
            // Carry any overshoot into the next phase so long gaps between ticks do not drift.
            TimeSpan overshoot = Remaining;
            LoadPhase(Phase == TimerPhase.Work ? TimerPhase.Break : TimerPhase.Work);
            Remaining += overshoot;
        }

        OnStateChanged();
    }

    private void OnTicked(object? sender, EventArgs e) => AdvanceToNow();

    private void AdvanceToNow()
    {
        TimeSpan now = _timeSource.Now;
        TimeSpan elapsed = now - _lastTimestamp;
        _lastTimestamp = now;
        Tick(elapsed);
    }

    private void LoadPhase(TimerPhase phase)
    {
        Phase = phase;
        CurrentPhaseDuration = phase == TimerPhase.Work ? WorkDuration : BreakDuration;
        Remaining = CurrentPhaseDuration;
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static void ValidateDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be positive.");
        }
    }
}
