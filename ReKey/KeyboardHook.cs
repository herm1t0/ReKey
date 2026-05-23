using System.Runtime.InteropServices;

namespace ReKey;

/// <summary>
/// Manages a low-level keyboard hook (WH_KEYBOARD_LL) to intercept and rebind keys.
/// The rebind list can be replaced atomically at runtime via <see cref="UpdateRebinds"/>.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    // Volatile ensures the hook callback always sees the latest reference after a swap.
    private volatile List<RebindEntry> _rebinds;

    // Maps source VK codes whose key-down was suppressed to the target VK code
    // currently being held.  When a key is auto-repeated, we do NOT re-send the
    // target key -- it is already down.  Key-up sends the corresponding target
    // key-up and removes the entry.
    private readonly Dictionary<ushort, ushort> _activeRebinds = [];

    private nint _hookHandle;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private bool _disposed;

    /// <summary>
    /// Optional callback for non-critical warnings (e.g. SendInput blocked by UIPI).
    /// </summary>
    public Action<string>? Warning { get; set; }

    public KeyboardHook(List<RebindEntry> rebinds)
    {
        _rebinds = rebinds;
        _proc = HookCallback;
    }

    public void Install()
    {
        // For WH_KEYBOARD_LL with dwThreadId=0, Windows only uses hMod to
        // locate the hook procedure for in-process dispatch. The module handle
        // of the current process is the correct value.
        //
        // Use MainModule.BaseAddress instead of GetModuleHandle(null) because
        // single-file-published apps may have a different host module layout;
        // MainModule always points to the host EXE that contains our code.
        nint moduleHandle;
        try
        {
            moduleHandle = System.Diagnostics.Process.GetCurrentProcess().MainModule!.BaseAddress;
        }
        catch
        {
            // Fallback for edge cases where MainModule is unavailable
            moduleHandle = NativeMethods.GetModuleHandleW(null);
        }

        _hookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            moduleHandle,
            0);

        if (_hookHandle != nint.Zero) return;
        var error = Marshal.GetLastWin32Error();
        throw new System.ComponentModel.Win32Exception(error,
            $"Failed to install keyboard hook. Error code: {error}");
    }

    public void Uninstall()
    {
        if (_hookHandle == nint.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = nint.Zero;
    }

    /// <summary>
    /// Atomically replaces the current rebind list with a new one.
    /// Safe to call from any thread; the hook callback will see the update
    /// on the next key event.
    /// </summary>
    public void UpdateRebinds(List<RebindEntry> newRebinds)
    {
        _rebinds = newRebinds;
    }

    /// <summary>
    /// The hook callback -- called by Windows for every keyboard event system-wide.
    /// </summary>
    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
            return NativeMethods.CallNextHookEx(nint.Zero, nCode, wParam, lParam);

        var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
        var vkCode = (ushort)hookStruct.vkCode;

        var isKeyDown = wParam is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
        var isKeyUp = wParam is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP;

        // Ignore events injected programmatically (e.g. our own SendInput).
        if ((hookStruct.flags & NativeMethods.LLKHF_INJECTED) != 0)
        {
            // Still block injected key-up for keys we suppressed,
            // otherwise the system would see a stray key-up.
            if (isKeyUp && _activeRebinds.Remove(vkCode, out _))
                return 1;

            return NativeMethods.CallNextHookEx(nint.Zero, nCode, wParam, lParam);
        }

        // Handle KEYUP for previously suppressed keys: send target key-up
        // and block the physical release of the source key.
        if (isKeyUp && _activeRebinds.Remove(vkCode, out var targetVk))
        {
            SendTargetKeyUp(targetVk);
            return 1;
        }

        // Pass through all other key-up events.
        if (isKeyUp)
            return NativeMethods.CallNextHookEx(nint.Zero, nCode, wParam, lParam);

        // KEYDOWN processing: check each rebind rule.
        foreach (var rebind in _rebinds)
        {
            if (vkCode != rebind.SourceKey)
                continue;

            // If the target is already held (auto-repeat), just suppress the
            // repeated key-down without sending another target key.
            if (_activeRebinds.ContainsKey(vkCode))
                return 1;

            _activeRebinds[vkCode] = rebind.TargetKey;
            SendTargetKeyDown(rebind.TargetKey);
            return 1;
        }

        return NativeMethods.CallNextHookEx(nint.Zero, nCode, wParam, lParam);
    }

    /// <summary>
    /// Sends a single key-down event for the target key via SendInput.
    /// </summary>
    private void SendTargetKeyDown(ushort targetVk)
    {
        var extended = VirtualKeyCodes.IsExtendedKey(targetVk);
        var input = BuildKeyInput(targetVk, NativeMethods.KEYEVENTF_KEYDOWN, extended);
        SendSingleInput(input, targetVk);
    }

    /// <summary>
    /// Sends a single key-up event for the target key via SendInput.
    /// </summary>
    private void SendTargetKeyUp(ushort targetVk)
    {
        var extended = VirtualKeyCodes.IsExtendedKey(targetVk);
        var input = BuildKeyInput(targetVk, NativeMethods.KEYEVENTF_KEYUP, extended);
        SendSingleInput(input, targetVk);
    }

    /// <summary>
    /// Sends one INPUT structure via SendInput and logs a warning on failure.
    /// </summary>
    private void SendSingleInput(NativeMethods.INPUT input, ushort targetVk)
    {
        var sent = NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());

        if (sent == 0)
        {
            var keyName = VirtualKeyCodes.TryGetName(targetVk, out var name)
                ? name
                : $"0x{targetVk:X2}";
            Warning?.Invoke(
                $"SendInput failed for {keyName}. " +
                "The target application may have higher privileges (UIPI block). " +
                "Try running ReKey as administrator.");
        }
    }

    /// <summary>
    /// Builds a single KEYBDINPUT wrapped in an INPUT structure.
    /// </summary>
    private static NativeMethods.INPUT BuildKeyInput(ushort vk, uint baseFlags, bool extended)
    {
        var flags = baseFlags;
        if (extended)
            flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        Uninstall();
        _activeRebinds.Clear();
        _disposed = true;
    }
}
