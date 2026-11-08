using Microsoft.UI.Xaml.Controls;
using PomodoroTimer.Core.ViewModels;

namespace PomodoroTimer.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsViewModel Vm { get; } = App.Current.SettingsViewModel;
}
