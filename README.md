# Codex Tray

Codex Tray is a small Windows notification-area monitor for Codex usage limits.

This is an unofficial community project and is not affiliated with OpenAI.

## Trust model

- Uses the documented local `codex app-server` JSONL protocol.
- Never reads `.codex/auth.json`.
- Never handles OAuth or API tokens.
- Makes no HTTP requests itself.
- Stores no account data, usage history, or credentials. It persists only the last weekly-window counters and reset-credit count needed to detect notifications.
- Has no updater. New builds are installed deliberately.
- Uses only the .NET Windows desktop framework; there are no NuGet dependencies.

The local Codex process remains responsible for authentication and its own OpenAI network access.

## Features

- Tray icon shows the percentage remaining in the short usage window, or the weekly window when that is the only limit reported.
- Menu shows short-window and weekly usage, reset times, plan, reset-credit count, and token summaries when Codex returns them.
- Refreshes every five minutes and supports manual refresh.
- Shows a Windows notification when a weekly window resets or the available reset-credit count increases.
- Optional per-user "Start with Windows" registration from the tray menu.

## Build and verify

```powershell
dotnet build .\src\CodexTray\CodexTray.csproj -c Release
dotnet run --project .\tests\CodexTray.Tests\CodexTray.Tests.csproj -c Release
dotnet run --project .\tools\CodexTray.Probe\CodexTray.Probe.csproj -c Release
```

The probe performs a read-only live request through the installed Codex CLI and prints a credential-free summary.

## Publish

```powershell
dotnet publish .\src\CodexTray\CodexTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\artifacts\publish
```

Run `CodexTray.exe` from the publish directory. No installer or administrator rights are required.

Windows may show an unknown-publisher warning because releases are not code-signed yet. Build from source if that warning is not acceptable for your environment.

## Codex discovery

Codex Tray checks, in order:

1. `CODEX_TRAY_CODEX_PATH`, if set to a `codex.exe` file.
2. The current user's Codex desktop installation.
3. `codex.exe` entries on `PATH`.

## License

MIT
