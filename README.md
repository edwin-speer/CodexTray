# CodexTray

CodexTray is a small Windows notification-area application that keeps your Codex usage and activity visible without opening a browser. It reads data through the locally installed Codex app server and does not handle your OpenAI credentials itself.

This unofficial community project is not affiliated with OpenAI.

## What CodexTray can do

- Show your daily and weekly Codex usage in accessible circular gauges.
- Show remaining reset credits and let you use one after an explicit confirmation.
- Display reset countdowns or local reset times.
- Show Codex activity through the colored border around the usage card.
- Notify you when the weekly window resets or a reset credit is added.
- Refresh automatically every five minutes, or every minute while the card is pinned.
- Pause polling while Windows is locked and refresh after unlock.
- Open the official Codex usage analytics page.
- Start automatically when you sign in to Windows.
- Follow Windows light, dark, high-contrast, DPI, and text-scaling settings.

Hover over the tray icon or left-click it to open the usage card. Right-click the icon for **Analytics**, **Start with Windows**, **Refresh now**, and **Exit**.

## Requirements

- Windows 10 or Windows 11.
- Codex installed and signed in. CodexTray supports the npm-installed Codex CLI, the Codex desktop installation, `codex.exe` on `PATH`, or an explicit `CODEX_TRAY_CODEX_PATH`.
- The [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for the recommended managed ZIP. Choose the Windows Desktop Runtime for your system; the SDK is not required.

## Install the recommended managed version

The managed package starts without a persistent command window and is the best option when Windows or company policy blocks the unsigned standalone executable.

1. Download [`CodexTray-v1.1.0-managed.zip`](https://github.com/edwin-speer/CodexTray/releases/download/v1.1.0/CodexTray-v1.1.0-managed.zip).
2. Install the .NET 10 Desktop Runtime if it is not already installed.
3. Create a permanent folder, for example:

   ```text
   %LOCALAPPDATA%\Programs\CodexTray
   ```

4. Right-click the downloaded ZIP, select **Extract All**, and extract every file into that folder.
5. Open the extracted folder and double-click `Start-CodexTray.vbs`.
6. Look for the CodexTray icon in the Windows notification area. It may be inside the hidden-icons menu.

> [!IMPORTANT]
> Do not open the ZIP and double-click `Start-CodexTray.vbs` inside the compressed folder. Windows then runs the script from a temporary location without the adjacent CodexTray files, so the application cannot start. Extract the entire ZIP first, then run the VBS file from the extracted folder.

Keep all extracted files together. Do not move or delete individual DLL or JSON files.

## Start CodexTray quickly

### Create a desktop shortcut

1. Open the extracted CodexTray folder.
2. Right-click `Start-CodexTray.vbs`.
3. On Windows 11, select **Show more options**.
4. Select **Send to** > **Desktop (create shortcut)**.
5. Rename the shortcut to `CodexTray` if desired.

If **Send to** is unavailable, right-click the desktop, select **New** > **Shortcut**, and use this location when you installed CodexTray in the recommended folder:

```text
wscript.exe "%LOCALAPPDATA%\Programs\CodexTray\Start-CodexTray.vbs"
```

The VBS launcher starts CodexTray invisibly, so no command window remains open.

### Start automatically with Windows

Right-click the CodexTray notification icon and enable **Start with Windows**. This adds a current-user startup entry and does not require administrator rights.

Install CodexTray in its permanent folder before enabling this option. If you move the folder later, disable and re-enable **Start with Windows** so the saved path is updated.

## Standalone executable

The release also contains [`CodexTray-v1.1.0-win-x64.exe`](https://github.com/edwin-speer/CodexTray/releases/download/v1.1.0/CodexTray-v1.1.0-win-x64.exe). It includes the .NET runtime and can be run directly, but it is not code-signed. Windows SmartScreen, Defender, or company application-control policy may therefore warn, block it, or require administrator assistance. CodexTray itself requests normal user privileges and does not require elevation.

## Activity status and Codex hooks

CodexTray installs local Codex lifecycle hooks so the usage card can indicate whether Codex is busy, waiting for input, or finished. After the first launch, Codex may ask you to review and trust those hooks. Run `/hooks` in Codex when prompted.

The usage and activity features require a Codex-backed sign-in. API-key-only or unrelated provider configurations do not expose the account usage data used by CodexTray.

## Privacy and local data

- CodexTray communicates with the local `codex app-server` JSONL protocol.
- Codex owns authentication and network access.
- CodexTray does not read `.codex/auth.json`, handle OAuth or API tokens, or make direct HTTP requests.
- Notification history is stored under `%LOCALAPPDATA%\CodexTray`.
- Display preferences and the optional startup entry are stored for the current Windows user.

## Troubleshooting

### Nothing happens after starting the VBS file

- Confirm that you extracted the entire ZIP before starting the VBS file.
- Confirm that the .NET 10 Desktop Runtime is installed.
- Confirm that Codex is installed and signed in.
- Keep `Start-CodexTray.vbs`, `CodexTray.dll`, `CodexTray.Core.dll`, and the JSON files in the same folder.
- Check the hidden-icons menu in the notification area; only one CodexTray instance can run at a time.

For a visible diagnostic error, open PowerShell in the extracted folder and run:

```powershell
dotnet .\CodexTray.dll
```

### Codex cannot be found

Set `CODEX_TRAY_CODEX_PATH` to the full path of `codex.exe`, then restart CodexTray.

### The startup entry stopped working

If the extracted folder was moved, right-click the tray icon, disable **Start with Windows**, and enable it again.

## Build and verify from source

```powershell
dotnet build .\src\CodexTray\CodexTray.csproj -c Release
dotnet run --project .\tests\CodexTray.Tests\CodexTray.Tests.csproj -c Release
dotnet run --project .\tools\CodexTray.Probe\CodexTray.Probe.csproj -c Release
```

Create the managed distribution:

```powershell
dotnet publish .\src\CodexTray\CodexTray.csproj -c Release --self-contained false -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false -o .\artifacts\CodexTray-managed
```

The probe makes a read-only request through the installed Codex CLI and prints a credential-free summary.

## Project

CodexTray was originally developed by Bear Stone Smart Home from [vCloudInfo.com](https://www.vcloudinfo.com). Read the [launch article](https://www.vcloudinfo.com/2026/08/codex-tray-monitor-openai-codex-usage-windows.html) or browse the larger [Bear Stone Smart Home repository](https://github.com/CCOSTAN/Home-AssistantConfig).

CodexTray is now further developed by Speer IT.

## License

MIT
