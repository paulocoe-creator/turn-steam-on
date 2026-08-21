using Microsoft.Win32;

namespace TurnSteamOn.Platform;

public interface IStartupEntryStore
{
    string? GetValue(string name);

    void SetValue(string name, string value);

    void DeleteValue(string name);
}

public sealed class WindowsStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string EntryName = "TurnSteamOn";

    private readonly IStartupEntryStore _store;
    private readonly string _executablePath;

    public WindowsStartupManager()
        : this(new RegistryStartupEntryStore(), Environment.ProcessPath
            ?? throw new InvalidOperationException("The application executable path is unavailable."))
    {
    }

    public WindowsStartupManager(IStartupEntryStore store, string executablePath)
    {
        _store = store;
        _executablePath = executablePath;
    }

    public bool IsEnabled => string.Equals(_store.GetValue(EntryName), Command, StringComparison.OrdinalIgnoreCase);

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            _store.SetValue(EntryName, Command);
        }
        else
        {
            _store.DeleteValue(EntryName);
        }
    }

    private string Command => $"\"{_executablePath}\"";

    private sealed class RegistryStartupEntryStore : IStartupEntryStore
    {
        public string? GetValue(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(name) as string;
        }

        public void SetValue(string name, string value)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            key?.SetValue(name, value);
        }

        public void DeleteValue(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}