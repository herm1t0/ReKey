# ReKey

A low-level keyboard remapper. Intercepts keys system-wide via `WH_KEYBOARD_LL` hook and
replaces them with configurable target keys.

## Usage

```
ReKey.exe           Start background daemon (installs keyboard hook)
ReKey.exe --kill    Signal the running instance to shut down
ReKey.exe --status  Check if the background process is running
ReKey.exe --reload  Signal the running instance to reload config
ReKey.exe --config  Open config file in default editor
ReKey.exe --register       Add current directory to user PATH
ReKey.exe --unregister     Remove current directory from user PATH
ReKey.exe --unregister --all  Remove ALL directories with ReKey.exe from PATH
ReKey.exe --version Show version information
ReKey.exe --help    Show this help
```

After `--register`, run just `rekey` from any terminal.

## Configuration

Config file: `%APPDATA%\ReKey\rekey`

Override the directory with `REKEY_CONFIG_HOME` environment variable.

Format (one entry per line):

```
; comments start with ; or #
SourceKey = TargetKey

; Examples:
LWin = F24
CapsLock = Escape
RAlt = Enter
```

After editing, run `ReKey.exe --reload` to apply changes without restarting.

## How it works

Installs a `WH_KEYBOARD_LL` hook and intercepts keystrokes before they reach applications.
When a source key is pressed, the corresponding target key is injected instead.

## Build

Requires .NET 10.0 SDK. Depends on [Herm1t.Shared](https://www.nuget.org/packages/Herm1t.Shared) NuGet package.

```powershell
dotnet publish -c Release
```

Target: `win-x64`, single-file publish.
