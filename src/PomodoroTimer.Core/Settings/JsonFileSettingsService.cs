using System.Diagnostics;
using System.Text.Json;

namespace PomodoroTimer.Core.Settings;

/// <summary>Persists <see cref="PomodoroSettings"/> as a JSON file.</summary>
public sealed class JsonFileSettingsService : ISettingsService
{
    private readonly string _filePath;

    public JsonFileSettingsService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public PomodoroSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new PomodoroSettings();
            }

            string json = File.ReadAllText(_filePath);
            PomodoroSettings? settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.PomodoroSettings);
            return (settings ?? new PomodoroSettings()).Normalize();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Failed to load settings from '{_filePath}': {ex.Message}");
            return new PomodoroSettings();
        }
    }

    public void Save(PomodoroSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temporary file first so a crash mid-write cannot corrupt the settings.
            string tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings.Normalize(), SettingsJsonContext.Default.PomodoroSettings));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Failed to save settings to '{_filePath}': {ex.Message}");
        }
    }
}
