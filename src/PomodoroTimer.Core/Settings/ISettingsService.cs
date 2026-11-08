namespace PomodoroTimer.Core.Settings;

public interface ISettingsService
{
    /// <summary>Loads saved settings, or defaults when none are saved or the data is unreadable.</summary>
    PomodoroSettings Load();

    void Save(PomodoroSettings settings);
}
