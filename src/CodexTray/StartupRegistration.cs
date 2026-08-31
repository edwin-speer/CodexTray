using Microsoft.Win32;
using System.Reflection;

namespace CodexTray;

internal static class StartupRegistration
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CodexTray";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
        return key?.GetValue(ValueName) is string value
               && string.Equals(value.Trim(), GetStartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)
                        ?? throw new InvalidOperationException("Could not open the current-user startup registry key.");
        if (enabled)
        {
            key.SetValue(ValueName, GetStartupCommand());
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string GetStartupCommand()
    {
        var executable = Environment.ProcessPath
                         ?? throw new InvalidOperationException("Could not determine the Codex Tray executable path.");
        if (!string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"{executable}\"";
        }

        var assembly = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrWhiteSpace(assembly))
        {
            throw new InvalidOperationException("Could not determine the Codex Tray assembly path.");
        }

        return $"\"{executable}\" \"{assembly}\"";
    }
}
