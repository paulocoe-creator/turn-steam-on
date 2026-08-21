namespace TurnSteamOn.Core;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public sealed record AppPreferences
{
    public const int CurrentSchemaVersion = 1;

    public AppPreferences(
        int schemaVersion,
        AppTheme theme,
        IReadOnlyCollection<DeviceSelection> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);

        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "The application theme is unknown.");
        }

        var deviceSnapshot = devices.ToArray();
        if (deviceSnapshot.Any(static device => device is null))
        {
            throw new ArgumentException("Device selections cannot contain null entries.", nameof(devices));
        }

        if (deviceSnapshot
            .Select(device => device.StableId)
            .Distinct(StringComparer.Ordinal)
            .Count() != deviceSnapshot.Length)
        {
            throw new ArgumentException("Device selections must have unique stable IDs.", nameof(devices));
        }

        SchemaVersion = schemaVersion;
        Theme = theme;
        Devices = deviceSnapshot;
    }

    public static AppPreferences Default => new(CurrentSchemaVersion, AppTheme.System, []);

    public int SchemaVersion { get; }

    public AppTheme Theme { get; }

    public IReadOnlyList<DeviceSelection> Devices { get; }
}

public interface IDevicePreferencesStore
{
    Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default);
}
