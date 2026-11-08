using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimer.Core.ViewModels;

namespace PomodoroTimer.Pages;

public sealed partial class TimerPage : Page
{
    public TimerPage()
    {
        InitializeComponent();
    }

    /// <summary>Shared app-wide view model, so the countdown survives navigation.</summary>
    public TimerViewModel Vm { get; } = App.Current.TimerViewModel;

    public static Visibility VisibleWhen(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility CollapsedWhen(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    // Start and Pause disable themselves when pressed. Hand focus to the counterpart button
    // so keyboard users do not lose focus. Click fires before the command runs, so defer
    // until the command has updated the buttons' enabled state.
    private void StartButton_Click(object sender, RoutedEventArgs e) => MoveFocusAfterCommand(PauseButton);

    private void PauseButton_Click(object sender, RoutedEventArgs e) => MoveFocusAfterCommand(StartButton);

    private void MoveFocusAfterCommand(Control target) =>
        DispatcherQueue.TryEnqueue(() => target.Focus(FocusState.Programmatic));
}
