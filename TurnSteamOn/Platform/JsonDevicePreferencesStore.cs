using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TurnSteamOn.Core;

namespace TurnSteamOn.Platform;

public sealed class JsonDevicePreferencesStore : IDevicePreferencesStore
{
    private const string PreferencesFileName = "preferences.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public JsonDevicePreferencesStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TurnSteamOn",
            PreferencesFileName))
    {
    }

    public JsonDevicePreferencesStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
    }

    public async Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            return AppPreferences.Default;
        }

        try
        {
            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var document = await JsonSerializer.DeserializeAsync<PreferencesDocument>(
                stream,
                SerializerOptions,
                cancellationToken);

            if (document is null)
            {
                throw new InvalidDataException("The preferences file does not contain valid JSON data.");
            }

            return ToPreferences(document);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The preferences file does not contain valid JSON data.", exception);
        }
    }

    public async Task SaveAsync(
        AppPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureSupportedSchema(preferences.SchemaVersion);

        await _writeGate.WaitAsync(cancellationToken);
        string? temporaryPath = null;

        try
        {
            var directory = Path.GetDirectoryName(_filePath)
                ?? throw new InvalidOperationException("The preferences directory is unavailable.");
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(
                directory,
                $"{PreferencesFileName}.{Guid.NewGuid():N}.tmp");

            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    ToDocument(preferences),
                    SerializerOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(_filePath))
            {
                File.Replace(temporaryPath, _filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, _filePath);
            }

            temporaryPath = null;
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            _writeGate.Release();
        }
    }

    private static AppPreferences ToPreferences(PreferencesDocument document)
    {
        EnsureSupportedSchema(document.SchemaVersion);

        if (document.Devices is null)
        {
            throw new InvalidDataException("The preferences file does not contain a device selection list.");
        }

        try
        {
            var devices = document.Devices
                .Select(device => new DeviceSelection(
                    device.StableId ?? string.Empty,
                    device.Enabled,
                    device.LastKnownName ?? string.Empty))
                .ToArray();

            return new AppPreferences(document.SchemaVersion, document.Theme, devices);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "The preferences file contains invalid device selection or theme data.",
                exception);
        }
    }

    private static PreferencesDocument ToDocument(AppPreferences preferences)
    {
        return new PreferencesDocument
        {
            SchemaVersion = preferences.SchemaVersion,
            Theme = preferences.Theme,
            Devices = preferences.Devices
                .Select(device => new DeviceSelectionDocument
                {
                    StableId = device.StableId,
                    Enabled = device.Enabled,
                    LastKnownName = device.LastKnownName
                })
                .ToList()
        };
    }

    private static void EnsureSupportedSchema(int schemaVersion)
    {
        if (schemaVersion != AppPreferences.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Preferences schema version {schemaVersion} is not supported.");
        }
    }

    private sealed class PreferencesDocument
    {
        public int SchemaVersion { get; init; }

        public AppTheme Theme { get; init; }

        public List<DeviceSelectionDocument>? Devices { get; init; }
    }

    private sealed class DeviceSelectionDocument
    {
        public string? StableId { get; init; }

        public bool Enabled { get; init; }

        public string? LastKnownName { get; init; }
    }
}
