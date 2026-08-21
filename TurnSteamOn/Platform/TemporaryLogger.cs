using System.Diagnostics;
using System.IO;

namespace TurnSteamOn.Platform;

internal static class TemporaryLogger
{
    private static readonly object Gate = new();

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TurnSteamOn",
        "turn-steam-on.log");

    public static void Log(string message, Exception? exception = null)
    {
        var entry = $"{DateTimeOffset.Now:O} {message}";
        if (exception is not null)
        {
            entry += $"{Environment.NewLine}{exception}";
        }

        Debug.WriteLine(entry);

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, entry + Environment.NewLine);
            }
        }
        catch (Exception loggingException)
        {
            Debug.WriteLine($"Unable to write temporary log: {loggingException}");
        }
    }

    public static void Error(string message, Exception exception)
    {
        Log($"ERROR: {message}", exception);
    }
}