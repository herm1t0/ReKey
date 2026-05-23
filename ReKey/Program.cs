using System.Diagnostics;
using System.Runtime.Versioning;
using Shared;

namespace ReKey;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static readonly CancellationTokenSource ShutdownCts = new();

    /// <summary>
    /// Entry point.
    ///
    /// Usage:
    ///   ReKey.exe             start in background (installs keyboard hook)
    ///   ReKey.exe --kill      signal the running instance to shut down
    ///   ReKey.exe --status    check if the background process is running
    ///   ReKey.exe --reload    signal the running instance to reload config
    ///   ReKey.exe --config    open config file in default editor
    /// </summary>
    private static int Main(string[] args)
    {
        var logDir = Environment.GetEnvironmentVariable("REKEY_CONFIG_HOME");
        Logger.Init("ReKey", string.IsNullOrWhiteSpace(logDir) ? null : logDir);

        return args.Length switch
        {
            > 0 when args[0].Equals("--kill", StringComparison.OrdinalIgnoreCase) => HandleKill(),
            > 0 when args[0].Equals("--status", StringComparison.OrdinalIgnoreCase) => HandleStatus(),
            > 0 when args[0].Equals("--reload", StringComparison.OrdinalIgnoreCase) => HandleReload(),
            > 0 when args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) => HandleHelp(),
            > 0 when args[0].Equals("--version", StringComparison.OrdinalIgnoreCase) => HandleVersion(),
            > 0 when args[0].Equals("--config", StringComparison.OrdinalIgnoreCase) => HandleConfig(),
            > 0 when args[0].Equals("--register", StringComparison.OrdinalIgnoreCase) => HandleRegister(),
            > 0 when args[0].Equals("--unregister", StringComparison.OrdinalIgnoreCase) => HandleUnregister(args),
            > 0 => HandleUnknown(args[0]),
            _ => StartBackground()
        };
    }

    /// <summary>
    /// Starts the application in background mode.
    /// </summary>
    private static int StartBackground()
    {
        // Ensure single instance
        using var singleInstance = new Shared.SingleInstance("ReKey", EventResetMode.ManualReset, reloadEvent: true);

        if (!singleInstance.IsFirstInstance)
        {
            // Another instance is already running
            return 1;
        }

        // Load configuration (create default if missing)
        var configPath = RebindConfig.CreateDefaultIfMissing();
        var rebinds = RebindConfig.Load();

        if (rebinds.Count == 0)
        {
            // Nothing to rebind — still run, waiting for config changes or shutdown
            Logger.Info($"No rebind rules found. Edit config at: {configPath}");
        }

        // Install keyboard hook
        using var hook = new KeyboardHook(rebinds);
        hook.Warning = Logger.Warn; // Log UIPI / SendInput failures
        hook.Install();

        // Capture the main thread's native ID so the watcher can post WM_QUIT to it
        var mainThreadId = NativeMethods.GetCurrentThreadId();

        // Start a background thread that waits for IPC signals (shutdown and reload)
        var ipcThread = new Thread(IpcWatcher)
        {
            IsBackground = true,
            Name = "IpcWatcher"
        };
        ipcThread.Start((
            ShutdownHandle: singleInstance.ShutdownHandle!,
            ReloadHandle: singleInstance.ReloadHandle!,
            MainThreadId: mainThreadId,
            Hook: hook
        ));

        // Run the Windows message pump (required for low-level hooks)
        // This blocks the current thread until PostQuitMessage is called
        RunMessagePump();

        // Cleanup
        ShutdownCts.Cancel();
        hook.Uninstall();

        return 0;
    }

    /// <summary>
    /// Background thread: waits for IPC signals (shutdown and reload).
    /// Shutdown posts WM_QUIT to the main thread.
    /// Reload re-reads the config file and atomically swaps the hook's rebind list.
    /// </summary>
    private static void IpcWatcher(object? state)
    {
        var (shutdownEvt, reloadEvt, mainThreadId, hook) =
            ((EventWaitHandle ShutdownHandle, EventWaitHandle ReloadHandle, uint MainThreadId, KeyboardHook Hook))
            state!;

        var handles = new[] { shutdownEvt, reloadEvt, ShutdownCts.Token.WaitHandle };

        while (true)
        {
            var signaled = WaitHandle.WaitAny(handles);

            switch (signaled)
            {
                // Shutdown
                case 0:
                {
                    const uint WM_QUIT = 0x0012;
                    // Post WM_QUIT to the MAIN thread (not this background thread!)
                    NativeMethods.PostThreadMessageW(mainThreadId, WM_QUIT, 0, 0);
                    return;
                }
                // Reload
                case 1:
                    try
                    {
                        var rebinds = RebindConfig.Load();
                        hook.UpdateRebinds(rebinds);
                        Logger.Info($"Config reloaded ({rebinds.Count} rebind(s))");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Config reload failed: {ex.Message}");
                    }

                    // Continue waiting for more signals
                    continue;
                default:
                    // Cancellation token
                    return;
            }
        }
    }

    /// <summary>
    /// Runs the Windows message loop.
    /// Required for WH_KEYBOARD_LL low-level hooks to function.
    /// </summary>
    private static void RunMessagePump()
    {
        // Use GetMessage - it blocks until a message arrives
        // WM_QUIT causes GetMessage to return false, exiting the loop
        while (NativeMethods.GetMessageW(out var msg, nint.Zero, 0, 0))
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessageW(ref msg);
        }
    }

    // - Handlers

    private static int HandleKill()
    {
        ConsoleHelper.EnsureConsole();
        try
        {
            if (Shared.SingleInstance.IsRunning("ReKey"))
            {
                Shared.SingleInstance.SignalShutdown("ReKey");
                Console.WriteLine("Shutdown signal sent.");
                return 0;
            }
            else
            {
                Console.WriteLine("ReKey is not running.");
                return 1;
            }
        }
        finally
        {
            Shared.NativeMethods.FreeConsole();
        }
    }

    private static int HandleStatus()
    {
        ConsoleHelper.EnsureConsole();
        try
        {
            if (Shared.SingleInstance.IsRunning("ReKey"))
            {
                Console.WriteLine("ReKey is running.");
                return 0;
            }
            else
            {
                Console.WriteLine("ReKey is not running.");
                return 1;
            }
        }
        finally
        {
            Shared.NativeMethods.FreeConsole();
        }
    }

    private static int HandleReload()
    {
        ConsoleHelper.EnsureConsole();
        try
        {
            if (Shared.SingleInstance.IsRunning("ReKey"))
            {
                var ok = Shared.SingleInstance.SignalReload("ReKey");
                Console.WriteLine(ok ? "Reload signal sent." : "Failed to send reload signal.");
                return ok ? 0 : 1;
            }
            else
            {
                Console.WriteLine("ReKey is not running. Start it first with: ReKey.exe");
                return 1;
            }
        }
        finally
        {
            Shared.NativeMethods.FreeConsole();
        }
    }

    private static int HandleHelp() => CliHandler.HandleHelp("""
                              ReKey - Keyboard Rebinder
                              =========================

                              Usage:
                                ReKey.exe                     Start in background mode (no visible window)
                                ReKey.exe --kill              Signal the running instance to shut down
                                ReKey.exe --status            Check if the background process is running
                                ReKey.exe --reload            Signal the running instance to reload config
                                ReKey.exe --config            Open config file in default editor
                                ReKey.exe --register          Add ReKey directory to user PATH
                                ReKey.exe --unregister        Remove ReKey directory from user PATH
                                ReKey.exe --unregister --all  Remove ALL user PATH entries with ReKey.exe
                                ReKey.exe --version           Show version information
                                ReKey.exe --help              Show this help message

                              Configuration:
                                Edit the file: %APPDATA%\ReKey\rekey
                                After editing, run: ReKey.exe --reload

                              Format (one entry per line):
                                ; comments start with ; or #
                                SourceKey = TargetKey

                              Examples:
                                LWin = F24
                                CapsLock = Escape
                                RAlt = Enter
                              """);

    private static int HandleVersion() => CliHandler.HandleVersion("ReKey");

    private static int HandleConfig()
    {
        ConsoleHelper.EnsureConsole();
        try
        {
            var configPath = RebindConfig.GetConfigPath();
            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Config file not found: {configPath}");
                Console.Error.WriteLine("Run ReKey once without arguments to create a default config, or create it manually.");
                return 1;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true
            });
            return 0;
        }
        finally
        {
            Shared.NativeMethods.FreeConsole();
        }
    }

    private static int HandleRegister() => CliHandler.HandleRegister();

    private static int HandleUnregister(string[] args) => CliHandler.HandleUnregister(args);

    private static int HandleUnknown(string arg) => CliHandler.HandleUnknown(arg);
}