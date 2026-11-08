using System.Text.Json.Serialization;

namespace PomodoroTimer.Core.Settings;

/// <summary>Source-generated JSON metadata so serialization is trim- and AOT-safe.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(PomodoroSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
