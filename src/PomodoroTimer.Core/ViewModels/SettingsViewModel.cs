using CommunityToolkit.Mvvm.ComponentModel;
using PomodoroTimer.Core.Settings;
using PomodoroTimer.Core.Timing;

namespace PomodoroTimer.Core.ViewModels;

/// <summary>Edits and persists user settings. Every change is saved immediately.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly PomodoroTimerService _timer;
    private PomodoroSettings _settings;

    public SettingsViewModel(ISettingsService settingsService, PomodoroTimerService timer, PomodoroSettings initialSettings)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(initialSettings);

        _settingsService = settingsService;
        _timer = timer;
        _settings = initialSettings.Normalize();

        // The change handlers ignore values equal to _settings, so this does not re-save.
        WorkMinutes = _settings.WorkMinutes;
        BreakMinutes = _settings.BreakMinutes;
        SelectedThemeIndex = (int)_settings.Theme;
    }

    /// <summary>Raised when the user picks a different theme.</summary>
    public event EventHandler<AppTheme>? ThemeChanged;

    public AppTheme Theme => _settings.Theme;

    /// <summary>Work duration in minutes. A double so it binds directly to NumberBox.Value.</summary>
    [ObservableProperty]
    public partial double WorkMinutes { get; set; }

    /// <summary>Break duration in minutes. A double so it binds directly to NumberBox.Value.</summary>
    [ObservableProperty]
    public partial double BreakMinutes { get; set; }

    /// <summary>Index of the selected theme option; matches the <see cref="AppTheme"/> values.</summary>
    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    partial void OnWorkMinutesChanged(double value)
    {
        if (NeedsCorrection(value, _settings.WorkMinutes, out int minutes))
        {
            WorkMinutes = minutes;
            return;
        }

        if (minutes != _settings.WorkMinutes)
        {
            UpdateSettings(_settings with { WorkMinutes = minutes });
            ApplyDurations();
        }
    }

    partial void OnBreakMinutesChanged(double value)
    {
        if (NeedsCorrection(value, _settings.BreakMinutes, out int minutes))
        {
            BreakMinutes = minutes;
            return;
        }

        if (minutes != _settings.BreakMinutes)
        {
            UpdateSettings(_settings with { BreakMinutes = minutes });
            ApplyDurations();
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (!Enum.IsDefined((AppTheme)value))
        {
            // Ignore "no selection" (-1) and out-of-range values by restoring the saved theme.
            SelectedThemeIndex = (int)_settings.Theme;
            return;
        }

        var theme = (AppTheme)value;
        if (theme != _settings.Theme)
        {
            UpdateSettings(_settings with { Theme = theme });
            ThemeChanged?.Invoke(this, theme);
        }
    }

    /// <summary>
    /// Converts a raw NumberBox value to whole, in-range minutes (an empty box, NaN, falls back
    /// to the saved value). Returns true when the value had to be corrected, so the caller
    /// writes the corrected value back to the property.
    /// </summary>
    private static bool NeedsCorrection(double value, int fallback, out int minutes)
    {
        minutes = double.IsNaN(value)
            ? fallback
            : PomodoroSettings.ClampMinutes((int)Math.Round(Math.Clamp(value, int.MinValue, int.MaxValue)));
        return minutes != value;
    }

    private void UpdateSettings(PomodoroSettings settings)
    {
        _settings = settings;
        _settingsService.Save(_settings);
    }

    private void ApplyDurations() =>
        _timer.SetDurations(TimeSpan.FromMinutes(_settings.WorkMinutes), TimeSpan.FromMinutes(_settings.BreakMinutes));
}
