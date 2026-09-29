using Microsoft.Win32;
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
        var assembly = Path.Combine(AppContext.BaseDirectory, "CodexTray.dll");
        if (!File.Exists(assembly))
        {
            throw new FileNotFoundException("Could not find the Codex Tray assembly.", assembly);
        }

        var dotnet = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet",
            "dotnet.exe");
        if (!File.Exists(dotnet))
        {
            throw new FileNotFoundException("Could not find dotnet.exe.", dotnet);
        }

        return $"\"{dotnet}\" \"{assembly}\"";
    }
}
