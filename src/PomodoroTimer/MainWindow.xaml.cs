using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimer.Core.Settings;
using PomodoroTimer.Pages;
using Windows.Graphics;

namespace PomodoroTimer;

public sealed partial class MainWindow : Window
{
    // Window size in device-independent pixels: compact navigation pane plus the timer content.
    private const int WindowWidthDip = 720;
    private const int WindowHeightDip = 640;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");

        nint hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        double scale = GetDpiForWindow(hwnd) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(WindowWidthDip * scale), (int)(WindowHeightDip * scale)));

        NavView.SelectedItem = TimerNavItem;
    }

    /// <summary>Applies the theme to all window content and to the caption buttons.</summary>
    public void ApplyTheme(AppTheme theme)
    {
        RootGrid.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        AppWindow.TitleBar.PreferredTheme = theme switch
        {
            AppTheme.Light => TitleBarTheme.Light,
            AppTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode,
        };
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        Type pageType = item.Tag switch
        {
            "timer" => typeof(TimerPage),
            "settings" => typeof(SettingsPage),
            _ => throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}"),
        };

        if (NavFrame.CurrentSourcePageType != pageType)
        {
            NavFrame.Navigate(pageType);

            // Navigation is driven by the NavigationView only, so no back stack is kept.
            NavFrame.BackStack.Clear();
        }
    }
}
