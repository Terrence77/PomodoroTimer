using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimer.Core.Timing;

namespace PomodoroTimer.Core.ViewModels;

/// <summary>Presents the Pomodoro timer state and its Start / Pause / Reset commands.</summary>
public sealed partial class TimerViewModel : ObservableObject
{
    private readonly PomodoroTimerService _timer;

    public TimerViewModel(PomodoroTimerService timer)
    {
        ArgumentNullException.ThrowIfNull(timer);
        _timer = timer;
        _timer.StateChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Remaining time formatted as "mm:ss".</summary>
    [ObservableProperty]
    public partial string RemainingText { get; private set; } = string.Empty;

    /// <summary>"Work" or "Break".</summary>
    [ObservableProperty]
    public partial string PhaseLabel { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsWorkPhase { get; private set; }

    /// <summary>Completed share of the current phase, from 0 to 100.</summary>
    [ObservableProperty]
    public partial double Progress { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
    public partial bool IsRunning { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    public partial bool CanReset { get; private set; }

    private bool CanStart => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start() => _timer.Start();

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Pause() => _timer.Pause();

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset() => _timer.Reset();

    private void Refresh()
    {
        RemainingText = TimeFormatter.FormatRemaining(_timer.Remaining);
        IsWorkPhase = _timer.Phase == TimerPhase.Work;
        PhaseLabel = IsWorkPhase ? "Work" : "Break";
        Progress = 100.0 * (1.0 - (_timer.Remaining / _timer.CurrentPhaseDuration));
        IsRunning = _timer.IsRunning;
        CanReset = !(_timer.IsAtPhaseStart && _timer.Phase == TimerPhase.Work);
    }
}
