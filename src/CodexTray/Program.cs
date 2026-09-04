namespace CodexTray;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--status-hook", StringComparer.OrdinalIgnoreCase))
        {
            CodexTray.Core.CodexActivityStore.UpdateFromHook(Console.In.ReadToEnd());
            return;
        }

        using var singleInstance = new Mutex(initiallyOwned: true, "Local\\CodexTray", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        var showTestNotification = args.Contains("--test-notification", StringComparer.OrdinalIgnoreCase);
        var showTestHover = args.Contains("--test-hover", StringComparer.OrdinalIgnoreCase);
        Application.Run(new TrayApplicationContext(showTestNotification, showTestHover));
    }
}
