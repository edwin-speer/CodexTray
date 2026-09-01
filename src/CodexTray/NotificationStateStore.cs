using System.Text.Json;
using CodexTray.Core;

namespace CodexTray;

internal sealed class NotificationStateStore
{
    private readonly string _path;

    public NotificationStateStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexTray");
        _path = Path.Combine(root, "notification-state.json");
    }

    public NotificationState? Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            return JsonSerializer.Deserialize<NotificationState>(File.ReadAllText(_path));
        }
        catch
        {
            return null;
        }
    }

    public void Save(NotificationState state)
    {
        try
        {
            var directory = Path.GetDirectoryName(_path)
                            ?? throw new InvalidOperationException("Notification state path has no parent directory.");
            Directory.CreateDirectory(directory);
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(state));
            File.Move(temporary, _path, overwrite: true);
        }
        catch (IOException)
        {
            // Notification history is optional; never hide fresh usage data because it cannot be saved.
        }
        catch (UnauthorizedAccessException)
        {
            // Notification history is optional; never hide fresh usage data because it cannot be saved.
        }
    }
}

