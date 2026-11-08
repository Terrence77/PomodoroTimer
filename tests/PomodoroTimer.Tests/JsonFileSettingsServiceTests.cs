using PomodoroTimer.Core.Settings;

namespace PomodoroTimer.Tests;

public sealed class JsonFileSettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PomodoroTimerTests", Guid.NewGuid().ToString("N"));
    private readonly string _filePath;

    public JsonFileSettingsServiceTests()
    {
        _filePath = Path.Combine(_directory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = new JsonFileSettingsService(_filePath).Load();

        Assert.Equal(25, settings.WorkMinutes);
        Assert.Equal(5, settings.BreakMinutes);
        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var saved = new PomodoroSettings { WorkMinutes = 50, BreakMinutes = 10, Theme = AppTheme.Dark };

        new JsonFileSettingsService(_filePath).Save(saved);
        var loaded = new JsonFileSettingsService(_filePath).Load();

        Assert.Equal(saved, loaded);
        Assert.False(File.Exists(_filePath + ".tmp"));
    }

    [Fact]
    public void Save_WritesThemeAsReadableString()
    {
        new JsonFileSettingsService(_filePath).Save(new PomodoroSettings { Theme = AppTheme.Light });

        Assert.Contains("\"Light\"", File.ReadAllText(_filePath));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{\"WorkMinutes\": \"abc\"}")]
    [InlineData("{\"Theme\": \"Purple\"}")]
    public void Load_CorruptFile_ReturnsDefaults(string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, content);

        var settings = new JsonFileSettingsService(_filePath).Load();

        Assert.Equal(new PomodoroSettings(), settings);
    }

    [Fact]
    public void Load_PartialFile_FillsMissingValuesWithDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, "{\"BreakMinutes\": 15}");

        var settings = new JsonFileSettingsService(_filePath).Load();

        Assert.Equal(25, settings.WorkMinutes);
        Assert.Equal(15, settings.BreakMinutes);
    }

    [Fact]
    public void Load_OutOfRangeValues_AreClamped()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, "{\"WorkMinutes\": 500, \"BreakMinutes\": -3, \"Theme\": 7}");

        var settings = new JsonFileSettingsService(_filePath).Load();

        Assert.Equal(120, settings.WorkMinutes);
        Assert.Equal(1, settings.BreakMinutes);
        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(60, 60)]
    [InlineData(120, 120)]
    [InlineData(121, 120)]
    [InlineData(-10, 1)]
    public void ClampMinutes_LimitsToOneThrough120(int input, int expected)
    {
        Assert.Equal(expected, PomodoroSettings.ClampMinutes(input));
    }
}
