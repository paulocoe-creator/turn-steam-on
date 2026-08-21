using TurnSteamOn.Core;
using TurnSteamOn.Platform;

namespace TurnSteamOn.Tests;

public sealed class JsonDevicePreferencesStoreTests
{
    [Fact]
    public async Task ReturnsDefaultsWhenThePreferencesFileDoesNotExist()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);

        var preferences = await store.LoadAsync();

        Assert.Equal(AppPreferences.CurrentSchemaVersion, preferences.SchemaVersion);
        Assert.Equal(AppTheme.System, preferences.Theme);
        Assert.Empty(preferences.Devices);
    }

    [Fact]
    public async Task SavesAndLoadsPreferencesWithoutLosingDeviceState()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);
        var expected = new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.Dark,
            [
                new DeviceSelection("bluetooth:controller-1", enabled: true, "Living room controller"),
                new DeviceSelection("bluetooth:controller-2", enabled: false, "Desk controller")
            ]);

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.Theme, actual.Theme);
        Assert.Equal(expected.Devices, actual.Devices);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public async Task RoundTripsEveryTheme(AppTheme theme)
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);
        var expected = new AppPreferences(AppPreferences.CurrentSchemaVersion, theme, []);

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(theme, actual.Theme);
    }

    [Fact]
    public async Task WritesTheVersionedHumanReadableSchema()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);

        await store.SaveAsync(new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.Light,
            [new DeviceSelection("bluetooth:controller-1", enabled: true, "Controller")]));
        var json = await File.ReadAllTextAsync(directory.FilePath);

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"theme\": \"Light\"", json);
        Assert.Contains("\"stableId\": \"bluetooth:controller-1\"", json);
    }

    [Fact]
    public async Task RejectsMalformedJson()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(directory.FilePath, "{ invalid json");
        var store = new JsonDevicePreferencesStore(directory.FilePath);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync());

        Assert.Contains("valid JSON", exception.Message);
    }

    [Fact]
    public async Task RejectsAnUnsupportedSchemaVersionWhenLoading()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(directory.FilePath, """
            {
              "schemaVersion": 2,
              "theme": "System",
              "devices": []
            }
            """);
        var store = new JsonDevicePreferencesStore(directory.FilePath);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => store.LoadAsync());

        Assert.Contains("version 2", exception.Message);
    }

    [Fact]
    public async Task RejectsAnUnsupportedSchemaVersionWhenSaving()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);
        var preferences = new AppPreferences(2, AppTheme.System, []);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => store.SaveAsync(preferences));

        Assert.Contains("version 2", exception.Message);
        Assert.False(File.Exists(directory.FilePath));
    }

    [Fact]
    public async Task RejectsInvalidDeviceData()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(directory.FilePath, """
            {
              "schemaVersion": 1,
              "theme": "System",
              "devices": [
                {
                  "stableId": "",
                  "enabled": true,
                  "lastKnownName": "Controller"
                }
              ]
            }
            """);
        var store = new JsonDevicePreferencesStore(directory.FilePath);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync());

        Assert.Contains("device selection", exception.Message);
    }

    [Fact]
    public async Task SerializesConcurrentWritesWithoutProducingPartialJson()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);
        var names = Enumerable.Range(1, 20).Select(index => $"Controller {index}").ToArray();

        var saves = names.Select(name => store.SaveAsync(new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.System,
            [new DeviceSelection("bluetooth:controller-1", enabled: true, name)])));

        await Task.WhenAll(saves);
        var actual = await store.LoadAsync();

        Assert.Contains(actual.Devices.Single().LastKnownName, names);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task HonorsCancellationWithoutReplacingExistingPreferences()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);
        var original = new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.Dark,
            [new DeviceSelection("bluetooth:controller-1", enabled: true, "Original")]);
        await store.SaveAsync(original);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(
            new AppPreferences(
                AppPreferences.CurrentSchemaVersion,
                AppTheme.Light,
                [new DeviceSelection("bluetooth:controller-1", enabled: false, "Replacement")]),
            cancellation.Token));
        var actual = await store.LoadAsync();

        Assert.Equal(AppTheme.Dark, actual.Theme);
        Assert.Equal("Original", actual.Devices.Single().LastKnownName);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task HonorsCancellationWhenLoadingDefaults()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonDevicePreferencesStore(directory.FilePath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync(cancellation.Token));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "TurnSteamOn.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string FilePath => System.IO.Path.Combine(Path, "preferences.json");

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
