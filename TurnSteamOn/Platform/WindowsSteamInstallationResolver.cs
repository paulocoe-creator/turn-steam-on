using Microsoft.Win32;
using System.IO;

namespace TurnSteamOn.Platform;

public sealed class WindowsSteamInstallationResolver
{
    public string? Resolve()
    {
        return FindExecutable(GetCandidates(), File.Exists);
    }

    public static string? FindExecutable(IEnumerable<string> candidates, Func<string, bool> fileExists)
    {
        return candidates.FirstOrDefault(fileExists);
    }

    private static IEnumerable<string> GetCandidates()
    {
        var registryRoots = new[]
        {
            ReadRegistryValue(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Valve\Steam", "SteamPath"),
            ReadRegistryValue(RegistryHive.LocalMachine, RegistryView.Registry64, @"Software\Valve\Steam", "InstallPath"),
            ReadRegistryValue(RegistryHive.LocalMachine, RegistryView.Registry32, @"Software\Valve\Steam", "InstallPath")
        };

        foreach (var root in registryRoots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            yield return Path.Combine(root!, "steam.exe");
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "Steam", "steam.exe");
        }

        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "Steam", "steam.exe");
        }
    }

    private static string? ReadRegistryValue(RegistryHive hive, RegistryView view, string subKey, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey);
            return key?.GetValue(valueName) as string;
        }
        catch (Exception exception)
        {
            TemporaryLogger.Error($"Unable to read Steam registry key '{subKey}'.", exception);
            return null;
        }
    }
}