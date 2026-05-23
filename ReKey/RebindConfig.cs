using Shared;

namespace ReKey;

/// <summary>
/// Represents a single key rebind entry from the configuration file.
/// Format: SourceKey = TargetKey
/// Example: LWin = F24
/// </summary>
internal sealed record RebindEntry
{
    /// <summary>The source key VK code that triggers the rebind.</summary>
    public ushort SourceKey { get; init; }

    /// <summary>The target key VK code to emit instead.</summary>
    public ushort TargetKey { get; init; }
}

/// <summary>
/// Parses the %APPDATA%\ReKey\rekey configuration file.
///
/// File format (one entry per line):
///   ; comments start with ; or #
///   SourceKey = TargetKey
/// </summary>
internal static class RebindConfig
{
    private const string ConfigFileName = "rekey";

    /// <summary>
    /// Loads and parses the configuration file from the ReKey config directory.
    /// Returns an empty list if the file does not exist.
    /// </summary>
    public static List<RebindEntry> Load()
    {
        var entries = new List<RebindEntry>();
        var configPath = GetConfigPath();

        if (!File.Exists(configPath))
            return entries;

        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(configPath))
        {
            lineNumber++;

            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            try
            {
                var entry = ParseLine(line);
                if (entry != null)
                    entries.Add(entry);
            }
            catch (Exception ex)
            {
                Logger.Error($"Config line {lineNumber}: {ex.Message}");
            }
        }

        // Remove entries that form circular rebind chains.
        RemoveCycles(entries);

        return entries;
    }

    /// <summary>
    /// Creates the config directory and writes a default config file with examples.
    /// Returns the path to the created file.
    /// </summary>
    public static string CreateDefaultIfMissing()
    {
        var configPath = GetConfigPath();

        if (File.Exists(configPath))
            return configPath;

        var dir = Path.GetDirectoryName(configPath)!;
        Directory.CreateDirectory(dir);

        File.WriteAllText(configPath, """
            ; ReKey configuration file
            ; Format:  SourceKey = TargetKey
            ;
            ; Examples:
            ; LWin = F24
            ; CapsLock = Escape
            ; RAlt = Enter

            """);

        return configPath;
    }

    /// <summary>
    /// Detects circular rebind chains and removes the entries that form them.
    ///
    /// A cycle exists when, e.g., A->B and B->A, or A->B, B->C, C->A.
    /// The last entry for a given source key wins (later lines override earlier).
    /// Entries involved in a cycle are logged as errors and removed.
    /// </summary>
    private static void RemoveCycles(List<RebindEntry> entries)
    {
        // Build the effective mapping (source -> target, last write wins).
        var map = new Dictionary<ushort, ushort>();
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var e = entries[i];
            if (!map.ContainsKey(e.SourceKey))
                map[e.SourceKey] = e.TargetKey;
        }

        // Find all keys that participate in a cycle.
        var cyclicKeys = new HashSet<ushort>();
        foreach (var source in map.Keys)
        {
            var visited = new HashSet<ushort> { source };
            var current = source;
            while (map.TryGetValue(current, out var next))
            {
                if (!visited.Add(next))
                {
                    // Cycle detected — mark all keys in this chain as cyclic.
                    foreach (var key in visited)
                        cyclicKeys.Add(key);
                    break;
                }
                current = next;
            }
        }

        if (cyclicKeys.Count == 0)
            return;

        // Remove entries whose source or target belongs to a cycle.
        entries.RemoveAll(e => cyclicKeys.Contains(e.SourceKey) || cyclicKeys.Contains(e.TargetKey));

        var keyNames = cyclicKeys.Select(k =>
            VirtualKeyCodes.TryGetName(k, out var name) ? name : $"0x{k:X2}");
        Logger.Error(
            $"Circular rebind detected involving keys: {string.Join(", ", keyNames)}. " +
            "All entries in the cycle have been removed.");
    }

    /// <summary>
    /// Full path to the config file. Uses the same directory as the logger
    /// (respects %REKEY_CONFIG_HOME% if set).
    /// </summary>
    public static string GetConfigPath()
    {
        return Path.Combine(Logger.AppDataDirectory, ConfigFileName);
    }

    /// <summary>
    /// Parses a single line of the config file.
    /// Format: "SourceKey = TargetKey"
    /// </summary>
    private static RebindEntry? ParseLine(string line)
    {
        var eqIndex = line.IndexOf('=');
        if (eqIndex < 0)
            throw new FormatException($"Missing '=' separator in: \"{line}\"");

        var left = line[..eqIndex].Trim();
        var right = line[(eqIndex + 1)..].Trim();

        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            throw new FormatException("Both source and target keys must be specified");

        if (!VirtualKeyCodes.TryParse(left, out var sourceVk))
            throw new FormatException($"Unknown source key: \"{left}\"");

        if (!VirtualKeyCodes.TryParse(right, out var targetVk))
            throw new FormatException($"Unknown target key: \"{right}\"");

        if (sourceVk == targetVk)
            throw new FormatException($"Source and target keys are the same: \"{left}\". This rebind would have no effect.");

        return new RebindEntry
        {
            SourceKey = sourceVk,
            TargetKey = targetVk
        };
    }
}
