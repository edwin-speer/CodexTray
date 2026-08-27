namespace CodexTray.Core;

public static class CodexLocator
{
    public static string Locate()
    {
        foreach (var candidate in Candidates())
        {
            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(candidate));
                if (Path.GetExtension(fullPath).Equals(".exe", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignore malformed environment and PATH entries.
            }
        }

        throw new FileNotFoundException(
            "codex.exe was not found. Install Codex or set CODEX_TRAY_CODEX_PATH to the full codex.exe path.");
    }

    private static IEnumerable<string> Candidates()
    {
        var explicitPath = Environment.GetEnvironmentVariable("CODEX_TRAY_CODEX_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            yield return explicitPath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(appData, "npm", "node_modules", "@openai", "codex", "node_modules", "@openai",
            "codex-win32-x64", "vendor", "x86_64-pc-windows-msvc", "bin", "codex.exe");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(localAppData, "Programs", "OpenAI", "Codex", "bin", "codex.exe");
        yield return Path.Combine(localAppData, "OpenAI", "Codex", "bin", "codex.exe");

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return Path.Combine(directory, "codex.exe");
        }
    }
}

