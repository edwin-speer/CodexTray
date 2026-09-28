Option Explicit

Dim shell, fso, appDir, dllPath, dotnetPath
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

appDir = fso.GetParentFolderName(WScript.ScriptFullName)
dllPath = fso.BuildPath(appDir, "CodexTray.dll")
dotnetPath = shell.ExpandEnvironmentStrings("%ProgramFiles%\dotnet\dotnet.exe")

shell.Run """" & dotnetPath & """ """ & dllPath & """", 0, False
