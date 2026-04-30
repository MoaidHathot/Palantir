using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Palantir;

/// <summary>
/// One row in the local registry tracking which personalities Palantir has
/// registered with Windows.
/// </summary>
public sealed class RegistryEntry
{
    [JsonPropertyName("name")]        public string Name { get; set; } = "";
    [JsonPropertyName("aumid")]       public string Aumid { get; set; } = "";
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("shortcut")]    public string Shortcut { get; set; } = "";
    [JsonPropertyName("icon")]        public string? Icon { get; set; }
    [JsonPropertyName("registeredAt")] public DateTimeOffset RegisteredAt { get; set; }
    [JsonPropertyName("source")]      public string? Source { get; set; }
}

internal sealed class RegistryFile
{
    [JsonPropertyName("entries")]
    public List<RegistryEntry> Entries { get; set; } = new();
}

/// <summary>
/// Snapshot of a personality's status for <c>personality list</c>.
/// </summary>
public sealed class PersonalityInfo
{
    public string Name { get; set; } = "";
    public string Aumid { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Icon { get; set; }
    public bool InConfig { get; set; }
    public bool RegisteredInWindows { get; set; }
    public string? ShortcutPath { get; set; }
}

/// <summary>
/// Manages personality registrations: shortcut creation, AUMID derivation,
/// the local tracking file, and bulk sync/prune operations.
/// </summary>
public static class PersonalityStore
{
    private const string DefaultAumidPrefix = "Palantir";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    // ── AUMID derivation ────────────────────────────────────────────

    public static string GetAumidPrefix(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        return string.IsNullOrWhiteSpace(config.AumidPrefix) ? DefaultAumidPrefix : config.AumidPrefix!;
    }

    /// <summary>Compute the AUMID for a named personality.</summary>
    public static string ComputeAumid(string personalityName, PalantirConfig? config = null)
    {
        var safe = SanitizeAumidSegment(personalityName);
        return $"{GetAumidPrefix(config)}.{safe}";
    }

    private static string SanitizeAumidSegment(string s) =>
        new(s.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.').ToArray());

    // ── Registry I/O ────────────────────────────────────────────────

    private static RegistryFile LoadRegistry(PalantirConfig? config = null)
    {
        var path = PathsResolver.GetRegistryFilePath(config);
        if (!File.Exists(path)) return new RegistryFile();
        try
        {
            return JsonSerializer.Deserialize<RegistryFile>(
                File.ReadAllText(path), ReadOptions) ?? new RegistryFile();
        }
        catch
        {
            return new RegistryFile();
        }
    }

    private static void SaveRegistry(RegistryFile registry, PalantirConfig? config = null)
    {
        var path = PathsResolver.GetRegistryFilePath(config);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, JsonSerializer.Serialize(registry, WriteOptions));
    }

    // ── Personality CRUD (config-side) ──────────────────────────────

    public static Personality? GetPersonality(string name)
    {
        var config = PresetStore.LoadConfig();
        return config.Personalities.TryGetValue(name, out var p) ? p : null;
    }

    public static void SavePersonality(string name, Personality personality)
    {
        var config = PresetStore.LoadConfig();
        config.Personalities[name] = personality;
        PresetStore.SaveConfig(config);
    }

    public static bool DeletePersonality(string name)
    {
        var config = PresetStore.LoadConfig();
        if (!config.Personalities.Remove(name)) return false;
        PresetStore.SaveConfig(config);
        return true;
    }

    // ── Windows registration ────────────────────────────────────────

    /// <summary>
    /// Ensure a personality is registered with Windows. Idempotent: returns
    /// existing entry unchanged if already present and matching.
    /// </summary>
    public static RegistryEntry Register(
        string name,
        Personality personality,
        PalantirConfig? config = null,
        Action<string>? onWarning = null)
    {
        if (string.IsNullOrWhiteSpace(personality.DisplayName))
            throw new ArgumentException(
                $"Personality \"{name}\" has no displayName.", nameof(personality));
        if (string.IsNullOrWhiteSpace(personality.Icon))
            throw new ArgumentException(
                $"Personality \"{name}\" has no icon.", nameof(personality));

        var aumid = ComputeAumid(name, config);
        var icoPath = IconCache.ResolveToIco(personality.Icon, config);
        var shortcut = GetShortcutPath(personality.DisplayName);
        var targetExe = ResolveTargetExe();

        ShellLink.Create(
            shortcutPath: shortcut,
            targetExe: targetExe,
            aumid: aumid,
            iconPath: icoPath,
            description: $"Palantir personality: {personality.DisplayName}");

        var entry = new RegistryEntry
        {
            Name = name,
            Aumid = aumid,
            DisplayName = personality.DisplayName,
            Shortcut = shortcut,
            Icon = icoPath,
            RegisteredAt = DateTimeOffset.Now,
            Source = PresetStore.GetConfigFilePath(),
        };

        var registry = LoadRegistry(config);
        registry.Entries.RemoveAll(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            e.Aumid.Equals(aumid, StringComparison.OrdinalIgnoreCase));
        registry.Entries.Add(entry);
        SaveRegistry(registry, config);

        return entry;
    }

    /// <summary>Unregister a personality by name. Returns true if removed.</summary>
    public static bool Unregister(
        string name,
        PalantirConfig? config = null,
        bool keepHistory = false,
        bool keepShortcut = false)
    {
        var registry = LoadRegistry(config);
        var entry = registry.Entries.FirstOrDefault(
            e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // Even without a registry entry, attempt to clean up by computed conventions.
        var aumid = entry?.Aumid ?? ComputeAumid(name, config);

        // Find shortcut path: from registry, or by personality lookup, or skip.
        var shortcut = entry?.Shortcut;
        if (string.IsNullOrEmpty(shortcut))
        {
            var personality = GetPersonality(name);
            if (personality is not null && !string.IsNullOrWhiteSpace(personality.DisplayName))
                shortcut = GetShortcutPath(personality.DisplayName!);
        }

        var removed = false;
        if (!keepShortcut && !string.IsNullOrEmpty(shortcut))
            removed = ShellLink.Delete(shortcut);

        if (!keepHistory)
        {
            try { ClearAumidHistory(aumid); }
            catch { /* best-effort */ }
        }

        if (entry is not null)
        {
            registry.Entries.Remove(entry);
            SaveRegistry(registry, config);
            return true;
        }
        return removed;
    }

    /// <summary>
    /// List personalities by combining config + Windows registry (our tracking file).
    /// </summary>
    public static List<PersonalityInfo> List(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        var registry = LoadRegistry(config);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<PersonalityInfo>();

        foreach (var (name, p) in config.Personalities)
        {
            var aumid = ComputeAumid(name, config);
            var entry = registry.Entries.FirstOrDefault(e =>
                e.Aumid.Equals(aumid, StringComparison.OrdinalIgnoreCase));
            result.Add(new PersonalityInfo
            {
                Name = name,
                Aumid = aumid,
                DisplayName = p.DisplayName,
                Icon = p.Icon,
                InConfig = true,
                RegisteredInWindows = entry is not null && File.Exists(entry.Shortcut),
                ShortcutPath = entry?.Shortcut,
            });
            seen.Add(name);
        }

        // Stale Windows entries (registered but missing from config).
        foreach (var entry in registry.Entries)
        {
            if (seen.Contains(entry.Name)) continue;
            result.Add(new PersonalityInfo
            {
                Name = entry.Name,
                Aumid = entry.Aumid,
                DisplayName = entry.DisplayName,
                Icon = entry.Icon,
                InConfig = false,
                RegisteredInWindows = File.Exists(entry.Shortcut),
                ShortcutPath = entry.Shortcut,
            });
        }

        return result;
    }

    // ── Bulk operations ─────────────────────────────────────────────

    public static List<RegistryEntry> RegisterAll(
        PalantirConfig? config = null,
        Action<string>? onWarning = null)
    {
        config ??= PresetStore.LoadConfig();
        var registered = new List<RegistryEntry>();
        foreach (var (name, p) in config.Personalities)
        {
            try
            {
                registered.Add(Register(name, p, config, onWarning));
            }
            catch (Exception ex)
            {
                onWarning?.Invoke($"Failed to register \"{name}\": {ex.Message}");
            }
        }
        return registered;
    }

    public static int UnregisterAll(
        PalantirConfig? config = null,
        bool keepHistory = false,
        Action<string>? onWarning = null)
    {
        config ??= PresetStore.LoadConfig();
        var registry = LoadRegistry(config);
        var prefix = GetAumidPrefix(config) + ".";
        var ours = registry.Entries
            .Where(e => e.Aumid.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var count = 0;
        foreach (var entry in ours)
        {
            try
            {
                if (Unregister(entry.Name, config, keepHistory: keepHistory))
                    count++;
            }
            catch (Exception ex)
            {
                onWarning?.Invoke($"Failed to unregister \"{entry.Name}\": {ex.Message}");
            }
        }
        return count;
    }

    /// <summary>
    /// Reconcile config ↔ Windows: register anything in config but not in Windows;
    /// unregister anything in Windows (under our prefix) but not in config.
    /// </summary>
    public static (int Registered, int Unregistered) Sync(
        PalantirConfig? config = null,
        bool keepHistory = false,
        Action<string>? onWarning = null)
    {
        config ??= PresetStore.LoadConfig();
        var infos = List(config);

        var registered = 0;
        var unregistered = 0;

        foreach (var info in infos)
        {
            if (info.InConfig && !info.RegisteredInWindows)
            {
                var p = config.Personalities[info.Name];
                try
                {
                    Register(info.Name, p, config, onWarning);
                    registered++;
                }
                catch (Exception ex)
                {
                    onWarning?.Invoke($"Failed to register \"{info.Name}\": {ex.Message}");
                }
            }
            else if (!info.InConfig && info.RegisteredInWindows)
            {
                try
                {
                    if (Unregister(info.Name, config, keepHistory: keepHistory))
                        unregistered++;
                }
                catch (Exception ex)
                {
                    onWarning?.Invoke($"Failed to unregister \"{info.Name}\": {ex.Message}");
                }
            }
        }

        return (registered, unregistered);
    }

    /// <summary>Remove only Windows registrations not present in config.</summary>
    public static int Prune(
        PalantirConfig? config = null,
        bool keepHistory = false,
        Action<string>? onWarning = null)
    {
        config ??= PresetStore.LoadConfig();
        var infos = List(config);
        var count = 0;
        foreach (var info in infos)
        {
            if (info.InConfig || !info.RegisteredInWindows) continue;
            try
            {
                if (Unregister(info.Name, config, keepHistory: keepHistory))
                    count++;
            }
            catch (Exception ex)
            {
                onWarning?.Invoke($"Failed to unregister \"{info.Name}\": {ex.Message}");
            }
        }
        return count;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    public static string GetShortcutPath(string displayName)
    {
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var safe = string.Concat(displayName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(
            startMenu, "Microsoft", "Windows", "Start Menu", "Programs", $"{safe}.lnk");
    }

    /// <summary>Path to the currently-running Palantir executable.</summary>
    public static string ResolveTargetExe()
    {
        var current = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? Assembly.GetEntryAssembly()?.Location
            ?? throw new InvalidOperationException("Cannot determine Palantir executable path.");
        return current;
    }

    private static void ClearAumidHistory(string aumid)
    {
        try
        {
            Windows.UI.Notifications.ToastNotificationManager.History.Clear(aumid);
        }
        catch
        {
            // Best-effort; some AUMIDs may not have history.
        }
    }
}
