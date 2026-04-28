using Microsoft.Win32;

namespace UltraPaste;

internal static class StartupManager
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "UltraPaste";

    internal static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(AppName) is string path &&
               path.Equals(GetExePath(), StringComparison.OrdinalIgnoreCase);
    }

    internal static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        key?.SetValue(AppName, GetExePath());
    }

    internal static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key?.GetValue(AppName) != null)
            key.DeleteValue(AppName);
    }

    private static string GetExePath() =>
        $"\"{Environment.ProcessPath}\"";
}
