namespace ReKey;

/// <summary>
/// Maps human-readable key names to Windows virtual key codes (VK_*).
/// </summary>
internal static class VirtualKeyCodes
{
    /// <summary>
    /// Tries to parse a key name (case-insensitive) to its virtual key code.
    /// Returns true on success, false if the key name is unknown.
    /// </summary>
    public static bool TryParse(string name, out ushort vkCode)
    {
        return KeyMap.TryGetValue(name.Trim(), out vkCode);
    }

    /// <summary>
    /// Reverse lookup: returns a human-readable name for the given VK code.
    /// Returns true on success, false if the VK code is unknown.
    /// </summary>
    public static bool TryGetName(ushort vkCode, out string name)
    {
        return ReverseKeyMap.TryGetValue(vkCode, out name!);
    }

    private static readonly Dictionary<string, ushort> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Modifiers ---
        ["LWin"] = 0x5B,       // VK_LWIN
        ["RWin"] = 0x5C,       // VK_RWIN
        ["LControl"] = 0xA2,   // VK_LCONTROL
        ["LCtrl"] = 0xA2,
        ["RControl"] = 0xA3,   // VK_RCONTROL
        ["RCtrl"] = 0xA3,
        ["LShift"] = 0xA0,     // VK_LSHIFT
        ["RShift"] = 0xA1,     // VK_RSHIFT
        ["LAlt"] = 0xA4,       // VK_LMENU
        ["RAlt"] = 0xA5,       // VK_RMENU
        ["LMenu"] = 0xA4,
        ["RMenu"] = 0xA5,

        // --- Generic modifiers (will be treated as left) ---
        ["Win"] = 0x5B,
        ["Ctrl"] = 0xA2,
        ["Control"] = 0xA2,
        ["Shift"] = 0xA0,
        ["Alt"] = 0xA4,

        // --- Function keys ---
        ["F1"] = 0x70,
        ["F2"] = 0x71,
        ["F3"] = 0x72,
        ["F4"] = 0x73,
        ["F5"] = 0x74,
        ["F6"] = 0x75,
        ["F7"] = 0x76,
        ["F8"] = 0x77,
        ["F9"] = 0x78,
        ["F10"] = 0x79,
        ["F11"] = 0x7A,
        ["F12"] = 0x7B,
        ["F13"] = 0x7C,
        ["F14"] = 0x7D,
        ["F15"] = 0x7E,
        ["F16"] = 0x7F,
        ["F17"] = 0x80,
        ["F18"] = 0x81,
        ["F19"] = 0x82,
        ["F20"] = 0x83,
        ["F21"] = 0x84,
        ["F22"] = 0x85,
        ["F23"] = 0x86,
        ["F24"] = 0x87,

        // --- Letters ---
        ["A"] = 0x41, ["B"] = 0x42, ["C"] = 0x43, ["D"] = 0x44, ["E"] = 0x45,
        ["F"] = 0x46, ["G"] = 0x47, ["H"] = 0x48, ["I"] = 0x49, ["J"] = 0x4A,
        ["K"] = 0x4B, ["L"] = 0x4C, ["M"] = 0x4D, ["N"] = 0x4E, ["O"] = 0x4F,
        ["P"] = 0x50, ["Q"] = 0x51, ["R"] = 0x52, ["S"] = 0x53, ["T"] = 0x54,
        ["U"] = 0x55, ["V"] = 0x56, ["W"] = 0x57, ["X"] = 0x58, ["Y"] = 0x59,
        ["Z"] = 0x5A,

        // --- Digits (top row) ---
        ["0"] = 0x30, ["1"] = 0x31, ["2"] = 0x32, ["3"] = 0x33, ["4"] = 0x34,
        ["5"] = 0x35, ["6"] = 0x36, ["7"] = 0x37, ["8"] = 0x38, ["9"] = 0x39,

        // --- Numpad ---
        ["NumPad0"] = 0x60, ["NumPad1"] = 0x61, ["NumPad2"] = 0x62,
        ["NumPad3"] = 0x63, ["NumPad4"] = 0x64, ["NumPad5"] = 0x65,
        ["NumPad6"] = 0x66, ["NumPad7"] = 0x67, ["NumPad8"] = 0x68,
        ["NumPad9"] = 0x69,
        ["NumPadMultiply"] = 0x6A,
        ["NumPadAdd"] = 0x6B,
        ["NumPadSeparator"] = 0x6C,
        ["NumPadSubtract"] = 0x6D,
        ["NumPadDecimal"] = 0x6E,
        ["NumPadDivide"] = 0x6F,

        // --- Navigation / Editing ---
        ["Backspace"] = 0x08,    // VK_BACK
        ["Tab"] = 0x09,
        ["Enter"] = 0x0D,        // VK_RETURN
        ["Return"] = 0x0D,
        ["Escape"] = 0x1B,
        ["Esc"] = 0x1B,
        ["Space"] = 0x20,
        ["PageUp"] = 0x21,       // VK_PRIOR
        ["PageDown"] = 0x22,     // VK_NEXT
        ["End"] = 0x23,
        ["Home"] = 0x24,
        ["Left"] = 0x25,
        ["Up"] = 0x26,
        ["Right"] = 0x27,
        ["Down"] = 0x28,
        ["PrintScreen"] = 0x2C,  // VK_SNAPSHOT
        ["Insert"] = 0x2D,
        ["Delete"] = 0x2E,
        ["Del"] = 0x2E,

        // --- Lock keys ---
        ["CapsLock"] = 0x14,
        ["NumLock"] = 0x90,
        ["ScrollLock"] = 0x91,

        // --- Symbols ---
        ["OemMinus"] = 0xBD,     // -
        ["OemPlus"] = 0xBB,      // =
        ["OemOpenBrackets"] = 0xDB,   // [
        ["OemCloseBrackets"] = 0xDD,  // ]
        ["OemPipe"] = 0xDC,           // \
        ["OemSemicolon"] = 0xBA,      // ;
        ["OemQuotes"] = 0xDE,         // '
        ["OemComma"] = 0xBC,          // ,
        ["OemPeriod"] = 0xBE,         // .
        ["OemQuestion"] = 0xBF,       // /
        ["OemTilde"] = 0xC0,          // `

        // --- Media keys (require KEYEVENTF_EXTENDEDKEY flag) ---
        ["VolumeMute"] = 0xAD,
        ["VolumeDown"] = 0xAE,
        ["VolumeUp"] = 0xAF,
        ["MediaNextTrack"] = 0xB0,
        ["MediaPrevTrack"] = 0xB1,
        ["MediaStop"] = 0xB2,
        ["MediaPlayPause"] = 0xB3,

        // --- Browser keys ---
        ["BrowserBack"] = 0xA6,
        ["BrowserForward"] = 0xA7,
        ["BrowserRefresh"] = 0xA8,
        ["BrowserStop"] = 0xA9,
        ["BrowserSearch"] = 0xAA,
        ["BrowserFavorites"] = 0xAB,
        ["BrowserHome"] = 0xAC,

        // --- Other ---
        ["Apps"] = 0x5D,         // VK_APPS (menu key)
        ["LaunchMail"] = 0xB4,
        ["LaunchMediaSelect"] = 0xB5,
        ["LaunchApp1"] = 0xB6,
        ["LaunchApp2"] = 0xB7,
    };

    /// <summary>
    /// Reverse mapping from VK code to primary key name.
    /// Built once from KeyMap. If multiple names map to the same code,
    /// the first one encountered wins (which is the canonical form).
    /// </summary>
    private static readonly Dictionary<ushort, string> ReverseKeyMap = BuildReverseMap();

    private static Dictionary<ushort, string> BuildReverseMap()
    {
        var reverse = new Dictionary<ushort, string>();
        foreach (var (name, vk) in KeyMap)
        {
            if (!reverse.ContainsKey(vk))
                reverse[vk] = name;
        }
        return reverse;
    }

    /// <summary>
    /// Keys that require the KEYEVENTF_EXTENDEDKEY flag when sent via SendInput.
    /// </summary>
    private static readonly HashSet<ushort> ExtendedKeys =
    [
        0x5B, 0x5C, // LWin, RWin
        0xA2, 0xA3, // LCtrl, RCtrl
        0xA0, 0xA1, // LShift, RShift
        0xA4, 0xA5, // LAlt, RAlt
        0x5D,       // Apps
        0x2C,       // PrintScreen
        0x2D,       // Insert
        0x2E,       // Delete
        0x21, 0x22, // PageUp, PageDown
        0x23, 0x24, // End, Home
        0x25, 0x26, 0x27, 0x28, // Arrows
        0xAD, 0xAE, 0xAF, // Volume
        0xB0, 0xB1, 0xB2, 0xB3, // Media
        0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, // Browser
        // Numpad keys
        0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
    ];

    /// <summary>
    /// Returns true if the virtual key code requires the extended key flag.
    /// </summary>
    public static bool IsExtendedKey(ushort vkCode) => ExtendedKeys.Contains(vkCode);

}
