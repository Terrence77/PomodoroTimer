using PomodoroTimer.Core.Settings;
using PomodoroTimer.Core.Timing;
using PomodoroTimer.Core.ViewModels;

namespace PomodoroTimer.Tests;

public sealed class ViewModelTests
{
    private readonly FakeTicker _ticker = new();
    private readonly FakeTimeSource _clock = new();
    private readonly PomodoroTimerService _timer;

    public ViewModelTests()
    {
        _timer = new PomodoroTimerService(_ticker, _clock, TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5));
    }

    private void Elapse(TimeSpan delta)
    {
        _clock.Advance(delta);
        _ticker.RaiseTick();
    }

    [Fact]
    public void TimerViewModel_InitialState()
    {
        var vm = new TimerViewModel(_timer);

        Assert.Equal("25:00", vm.RemainingText);
        Assert.Equal("Work", vm.PhaseLabel);
        Assert.True(vm.IsWorkPhase);
        Assert.True(vm.StartCommand.CanExecute(null));
        Assert.False(vm.PauseCommand.CanExecute(null));
        Assert.False(vm.ResetCommand.CanExecute(null));
    }

    [Fact]
    public void TimerViewModel_CommandsTrackRunningState()
    {
        var vm = new TimerViewModel(_timer);

        vm.StartCommand.Execute(null);
        Assert.False(vm.StartCommand.CanExecute(null));
        Assert.True(vm.PauseCommand.CanExecute(null));
        Assert.True(vm.ResetCommand.CanExecute(null));

        Elapse(TimeSpan.FromSeconds(61));
        Assert.Equal("23:59", vm.RemainingText);

        vm.PauseCommand.Execute(null);
        Assert.True(vm.StartCommand.CanExecute(null));
        Assert.False(vm.PauseCommand.CanExecute(null));
        Assert.True(vm.ResetCommand.CanExecute(null));

        vm.ResetCommand.Execute(null);
        Assert.Equal("25:00", vm.RemainingText);
        Assert.False(vm.ResetCommand.CanExecute(null));
    }

    [Fact]
    public void TimerViewModel_ShowsBreakPhaseAfterWorkEnds()
    {
        var vm = new TimerViewModel(_timer);
        vm.StartCommand.Execute(null);

        Elapse(TimeSpan.FromMinutes(25));

        Assert.Equal("Break", vm.PhaseLabel);
        Assert.False(vm.IsWorkPhase);
        Assert.Equal("05:00", vm.RemainingText);
        Assert.Equal(0, vm.Progress);
    }

    [Fact]
    public void SettingsViewModel_ChangingDurations_SavesAndUpdatesTimer()
    {
        var store = new InMemorySettingsService();
        var vm = new SettingsViewModel(store, _timer, new PomodoroSettings());
        Assert.Equal(0, store.SaveCount);

        vm.WorkMinutes = 30;
        vm.BreakMinutes = 10;

        Assert.Equal(30, store.Stored.WorkMinutes);
        Assert.Equal(10, store.Stored.BreakMinutes);
        Assert.Equal(TimeSpan.FromMinutes(30), _timer.WorkDuration);
        Assert.Equal(TimeSpan.FromMinutes(10), _timer.BreakDuration);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 120)]
    [InlineData(12.6, 13)]
    [InlineData(double.NaN, 25)]
    public void SettingsViewModel_InvalidWorkMinutes_AreCorrected(double input, int expected)
    {
        var store = new InMemorySettingsService();
        var vm = new SettingsViewModel(store, _timer, new PomodoroSettings());

        vm.WorkMinutes = input;

        Assert.Equal(expected, vm.WorkMinutes);
        Assert.Equal(expected, store.Stored.WorkMinutes);
    }

    [Fact]
    public void SettingsViewModel_ChangingTheme_SavesAndRaisesEvent()
    {
        var store = new InMemorySettingsService();
        var vm = new SettingsViewModel(store, _timer, new PomodoroSettings { Theme = AppTheme.Light });
        AppTheme? raised = null;
        vm.ThemeChanged += (_, theme) => raised = theme;

        vm.SelectedThemeIndex = (int)AppTheme.Dark;

        Assert.Equal(AppTheme.Dark, raised);
        Assert.Equal(AppTheme.Dark, store.Stored.Theme);
        Assert.Equal(AppTheme.Dark, vm.Theme);
    }

    [Fact]
    public void SettingsViewModel_InvalidThemeIndex_RestoresSavedTheme()
    {
        var store = new InMemorySettingsService();
        var vm = new SettingsViewModel(store, _timer, new PomodoroSettings { Theme = AppTheme.Dark });

        vm.SelectedThemeIndex = -1;

        Assert.Equal((int)AppTheme.Dark, vm.SelectedThemeIndex);
        Assert.Equal(0, store.SaveCount);
    }
}
