# ReKey

Low-level keyboard rebinder. Intercepts keys system-wide via `WH_KEYBOARD_LL` hook and replaces them with configurable target keys.

## Usage

```
ReKey.exe           Start in background (no window)
ReKey.exe --kill    Signal the running instance to shut down
ReKey.exe --status  Check if the background process is running
ReKey.exe --reload  Signal the running instance to reload config
ReKey.exe --version Show version information
ReKey.exe --help    Show help
```

## Configuration

Edit `%APPDATA%\ReKey\rekey`. After editing, run `ReKey.exe --reload`.

Format (one entry per line):

```
; comments start with ; or #
SourceKey = TargetKey

; Examples:
LWin = F24
CapsLock = Escape
RAlt = Enter
```

## Build

Requires .NET 10.0 SDK. Depends on [Herm1t.Shared](https://www.nuget.org/packages/Herm1t.Shared) NuGet package.

```powershell
dotnet publish -c Release
```

Target: `win-x64`, single-file publish.
