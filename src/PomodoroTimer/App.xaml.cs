using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using PomodoroTimer.Core.Settings;
using PomodoroTimer.Core.Timing;
using PomodoroTimer.Core.ViewModels;
using PomodoroTimer.Services;
using Windows.Storage;

namespace PomodoroTimer;

/// <summary>
/// Application entry point and composition root. The view models are created once here so
/// the timer keeps running while the user navigates between pages.
/// </summary>
public partial class App : Application
{
    private const string SettingsFileName = "settings.json";

    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    public static new App Current => (App)Application.Current;

    public TimerViewModel TimerViewModel { get; private set; } = null!;

    public SettingsViewModel SettingsViewModel { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settingsService = new JsonFileSettingsService(GetSettingsFilePath());
        PomodoroSettings settings = settingsService.Load();

        var timer = new PomodoroTimerService(
            new DispatcherQueueTicker(DispatcherQueue.GetForCurrentThread()),
            new StopwatchTimeSource(),
            TimeSpan.FromMinutes(settings.WorkMinutes),
            TimeSpan.FromMinutes(settings.BreakMinutes));

        TimerViewModel = new TimerViewModel(timer);
        SettingsViewModel = new SettingsViewModel(settingsService, timer, settings);

        _window = new MainWindow();
        _window.ApplyTheme(settings.Theme);
        SettingsViewModel.ThemeChanged += (_, theme) => _window.ApplyTheme(theme);
        _window.Activate();
    }

    private static string GetSettingsFilePath()
    {
        string folder;
        try
        {
            folder = ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException)
        {
            // No package identity (for example an unpackaged debug run): use the per-user app data folder.
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PomodoroTimer");
        }

        return Path.Combine(folder, SettingsFileName);
    }
}
