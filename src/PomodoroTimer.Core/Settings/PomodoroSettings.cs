namespace PomodoroTimer.Core.Settings;

/// <summary>User-configurable settings that persist across launches.</summary>
/// <remarks>
/// Positional so the JSON deserializer uses the constructor's default values for any
/// property missing from the file.
/// </remarks>
public sealed record PomodoroSettings(
    int WorkMinutes = PomodoroSettings.DefaultWorkMinutes,
    int BreakMinutes = PomodoroSettings.DefaultBreakMinutes,
    AppTheme Theme = AppTheme.System)
{
    public const int DefaultWorkMinutes = 25;
    public const int DefaultBreakMinutes = 5;
    public const int MinMinutes = 1;
    public const int MaxMinutes = 120;

    public static int ClampMinutes(int minutes) => Math.Clamp(minutes, MinMinutes, MaxMinutes);

    /// <summary>Returns a copy with durations clamped to the allowed range and a valid theme.</summary>
    public PomodoroSettings Normalize() => this with
    {
        WorkMinutes = ClampMinutes(WorkMinutes),
        BreakMinutes = ClampMinutes(BreakMinutes),
        Theme = Enum.IsDefined(Theme) ? Theme : AppTheme.System,
    };
}
