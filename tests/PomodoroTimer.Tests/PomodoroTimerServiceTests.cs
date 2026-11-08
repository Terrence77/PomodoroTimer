using PomodoroTimer.Core.Timing;

namespace PomodoroTimer.Tests;

public sealed class PomodoroTimerServiceTests
{
    private static readonly TimeSpan Work = TimeSpan.FromMinutes(25);
    private static readonly TimeSpan Break = TimeSpan.FromMinutes(5);

    private readonly FakeTicker _ticker = new();
    private readonly FakeTimeSource _clock = new();
    private readonly PomodoroTimerService _timer;

    public PomodoroTimerServiceTests()
    {
        _timer = new PomodoroTimerService(_ticker, _clock, Work, Break);
    }

    private void Elapse(TimeSpan delta)
    {
        _clock.Advance(delta);
        _ticker.RaiseTick();
    }

    [Fact]
    public void InitialState_IsIdleWorkPhaseWithFullDuration()
    {
        Assert.Equal(TimerPhase.Work, _timer.Phase);
        Assert.Equal(Work, _timer.Remaining);
        Assert.False(_timer.IsRunning);
        Assert.True(_timer.IsAtPhaseStart);
    }

    [Fact]
    public void Start_SetsRunningAndStartsTicker()
    {
        _timer.Start();

        Assert.True(_timer.IsRunning);
        Assert.True(_ticker.IsStarted);
    }

    [Fact]
    public void Tick_DecreasesRemainingByMeasuredElapsedTime()
    {
        _timer.Start();

        Elapse(TimeSpan.FromSeconds(1));
        Elapse(TimeSpan.FromMilliseconds(1500));

        Assert.Equal(Work - TimeSpan.FromSeconds(2.5), _timer.Remaining);
    }

    [Fact]
    public void Tick_UsesClockRatherThanTickCount_SoIrregularTicksDoNotDrift()
    {
        _timer.Start();

        // Ten ticks that together span exactly 60 seconds.
        for (int i = 0; i < 10; i++)
        {
            Elapse(TimeSpan.FromSeconds(i % 2 == 0 ? 4 : 8));
        }

        Assert.Equal(Work - TimeSpan.FromMinutes(1), _timer.Remaining);
    }

    [Fact]
    public void Tick_DirectCall_DecreasesRemaining()
    {
        _timer.Start();

        _timer.Tick(TimeSpan.FromSeconds(10));

        Assert.Equal(Work - TimeSpan.FromSeconds(10), _timer.Remaining);
    }

    [Fact]
    public void Pause_StopsDecrementing()
    {
        _timer.Start();
        Elapse(TimeSpan.FromSeconds(3));

        _timer.Pause();
        TimeSpan pausedAt = _timer.Remaining;
        Elapse(TimeSpan.FromSeconds(30));
        _timer.Tick(TimeSpan.FromSeconds(30));

        Assert.False(_timer.IsRunning);
        Assert.False(_ticker.IsStarted);
        Assert.Equal(pausedAt, _timer.Remaining);
    }

    [Fact]
    public void Pause_AccountsForTimeSinceLastTick()
    {
        _timer.Start();
        _clock.Advance(TimeSpan.FromMilliseconds(700));

        _timer.Pause();

        Assert.Equal(Work - TimeSpan.FromMilliseconds(700), _timer.Remaining);
    }

    [Fact]
    public void Resume_AfterPause_DoesNotCountPausedTime()
    {
        _timer.Start();
        Elapse(TimeSpan.FromSeconds(10));
        _timer.Pause();
        _clock.Advance(TimeSpan.FromMinutes(3));

        _timer.Start();
        Elapse(TimeSpan.FromSeconds(5));

        Assert.Equal(Work - TimeSpan.FromSeconds(15), _timer.Remaining);
    }

    [Fact]
    public void Reset_StopsAndRestoresFullWorkPhase()
    {
        _timer.Start();
        Elapse(Work + TimeSpan.FromSeconds(30));
        Assert.Equal(TimerPhase.Break, _timer.Phase);

        _timer.Reset();

        Assert.False(_timer.IsRunning);
        Assert.False(_ticker.IsStarted);
        Assert.Equal(TimerPhase.Work, _timer.Phase);
        Assert.Equal(Work, _timer.Remaining);
    }

    [Fact]
    public void ReachingZero_SwitchesWorkToBreak_AndKeepsRunning()
    {
        _timer.Start();

        Elapse(Work);

        Assert.Equal(TimerPhase.Break, _timer.Phase);
        Assert.Equal(Break, _timer.Remaining);
        Assert.True(_timer.IsRunning);
    }

    [Fact]
    public void ReachingZero_SwitchesBreakBackToWork()
    {
        _timer.Start();
        Elapse(Work);

        Elapse(Break);

        Assert.Equal(TimerPhase.Work, _timer.Phase);
        Assert.Equal(Work, _timer.Remaining);
        Assert.True(_timer.IsRunning);
    }

    [Fact]
    public void Overshoot_CarriesIntoNextPhase()
    {
        _timer.Start();

        Elapse(Work + TimeSpan.FromSeconds(2));

        Assert.Equal(TimerPhase.Break, _timer.Phase);
        Assert.Equal(Break - TimeSpan.FromSeconds(2), _timer.Remaining);
    }

    [Fact]
    public void LargeElapsed_CanSkipThroughMultiplePhases()
    {
        _timer.Start();

        Elapse(Work + Break + TimeSpan.FromMinutes(1));

        Assert.Equal(TimerPhase.Work, _timer.Phase);
        Assert.Equal(Work - TimeSpan.FromMinutes(1), _timer.Remaining);
    }

    [Fact]
    public void StateChanged_IsRaisedOnStartTickPauseReset()
    {
        int count = 0;
        _timer.StateChanged += (_, _) => count++;

        _timer.Start();
        Elapse(TimeSpan.FromSeconds(1));
        _timer.Pause();
        _timer.Reset();

        Assert.Equal(4, count);
    }

    [Fact]
    public void SetDurations_WhileRunning_DoesNotInterruptCurrentCountdown()
    {
        _timer.Start();
        Elapse(TimeSpan.FromMinutes(1));

        _timer.SetDurations(TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromMinutes(24), _timer.Remaining);
        Elapse(TimeSpan.FromMinutes(24));
        Assert.Equal(TimerPhase.Break, _timer.Phase);
        Assert.Equal(TimeSpan.FromMinutes(10), _timer.Remaining);
    }

    [Fact]
    public void SetDurations_ThenReset_UsesNewWorkDuration()
    {
        _timer.Start();
        Elapse(TimeSpan.FromMinutes(1));
        _timer.SetDurations(TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(10));

        _timer.Reset();

        Assert.Equal(TimeSpan.FromMinutes(50), _timer.Remaining);
    }

    [Fact]
    public void SetDurations_WhenIdleAtPhaseStart_AppliesImmediately()
    {
        _timer.SetDurations(TimeSpan.FromMinutes(30), Break);

        Assert.Equal(TimeSpan.FromMinutes(30), _timer.Remaining);
    }

    [Fact]
    public void SetDurations_WhenPausedMidPhase_KeepsRemaining()
    {
        _timer.Start();
        Elapse(TimeSpan.FromMinutes(1));
        _timer.Pause();

        _timer.SetDurations(TimeSpan.FromMinutes(30), Break);

        Assert.Equal(TimeSpan.FromMinutes(24), _timer.Remaining);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveDurations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroTimerService(_ticker, _clock, TimeSpan.Zero, Break));
    }
}
