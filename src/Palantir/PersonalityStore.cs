using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Palantir;

/// <summary>
/// One row in the local registry tracking which personalities Palantir has
/// registered with Windows. Stores logical / portable info only — concrete
/// shortcut and icon paths are recomputed on demand.
/// </summary>
public sealed class RegistryEntry
{
    [JsonPropertyName("name")]         public string Name { get; set; } = "";
    [JsonPropertyName("aumid")]        public string Aumid { get; set; } = "";
    [JsonPropertyName("displayName")]  public string? DisplayName { get; set; }

    /// <summary>
    /// The icon source as configured (file path, URL, or path with tokens).
    /// Concrete cached .ico path is recomputed via <see cref="IconCache"/> on demand.
    /// </summary>
    [JsonPropertyName("iconSource")]   public string? IconSource { get; set; }

    [JsonPropertyName("registeredAt")] public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>Computed Start Menu shortcut path (not persisted).</summary>
    [JsonIgnore]
    public string ShortcutPath =>
        PersonalityStore.GetShortcutPath(DisplayName ?? Name);
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

    /// <summary>
    /// Stable, app-wide CLSID published as the toast activator on every
    /// personality shortcut. Windows 10 1607+ requires the shortcut to advertise
    /// SOME registered ToastActivatorCLSID for <c>ToastNotificationManager.
    /// CreateToastNotifier(aumid).Show()</c> to actually display a notification.
    /// We don't need to handle out-of-process activations (in-process
    /// <c>Activated</c>/<c>Dismissed</c>/<c>Failed</c> events fire regardless),
    /// so a single stub CLSID shared by all personalities is sufficient.
    /// </summary>
    public static readonly Guid ToastActivatorClsid =
        new("3F2A9C4D-7B6E-4D9A-8C1E-5B3F7A9D2E4C");

    /// <summary>
    /// Conventional name of the built-in default personality. Auto-registered
    /// when no other personality is in play; user can fully customize by adding
    /// a <c>personalities.palantir</c> entry to <c>palantir.json</c>.
    /// </summary>
    public const string BuiltInDefaultName = "palantir";

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

    /// <summary>
    /// Resolve the built-in default personality, layering any user-provided
    /// <c>personalities.palantir</c> overrides from config on top of the
    /// built-in defaults (display name "Palantir", icon = target exe's
    /// embedded icon).
    /// </summary>
    public static Personality GetDefaultPersonality(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        config.Personalities.TryGetValue(BuiltInDefaultName, out var user);
        return new Personality
        {
            DisplayName = !string.IsNullOrWhiteSpace(user?.DisplayName)
                ? user.DisplayName
                : "Palantir",
            // Null icon → Register() falls back to the target exe's embedded icon.
            Icon = user?.Icon,
        };
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

        config ??= PresetStore.LoadConfig();
        var aumid = ComputeAumid(name, config);
        var targetExe = ResolveTargetExe();

        // Icon resolution:
        //   - If null/empty: use the target exe's embedded icon (Windows reads
        //     icon index 0). Useful for the built-in default personality.
        //   - If .exe / .dll: use directly (Windows reads embedded icon).
        //   - Otherwise: convert/cache through IconCache to a .ico file.
        var iconSource = personality.Icon;
        string iconPath;
        if (string.IsNullOrWhiteSpace(iconSource))
        {
            iconPath = targetExe;
            iconSource = null;
        }
        else
        {
            var expanded = PathsResolver.ExpandValue(iconSource, config);
            if (expanded.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                || expanded.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                iconPath = expanded;
            }
            else
            {
                iconPath = IconCache.ResolveToIco(iconSource, config);
            }
        }

        var shortcut = GetShortcutPath(personality.DisplayName!);

        // Replace any previously-registered shortcut for this personality
        // (handles displayName changes leaving an orphan).
        var registry = LoadRegistry(config);
        var prior = registry.Entries.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            e.Aumid.Equals(aumid, StringComparison.OrdinalIgnoreCase));
        if (prior is not null)
        {
            var priorShortcut = prior.ShortcutPath;
            if (!string.Equals(priorShortcut, shortcut, StringComparison.OrdinalIgnoreCase))
                ShellLink.Delete(priorShortcut);
            registry.Entries.Remove(prior);
        }

        ShellLink.Create(
            shortcutPath: shortcut,
            targetExe: targetExe,
            aumid: aumid,
            iconPath: iconPath,
            toastActivatorClsid: ToastActivatorClsid,
            description: $"Palantir personality: {personality.DisplayName}");

        EnsureToastActivatorRegistered(targetExe);
        RegisterAumid(aumid, personality.DisplayName!, iconPath);

        var entry = new RegistryEntry
        {
            Name = name,
            Aumid = aumid,
            DisplayName = personality.DisplayName,
            IconSource = iconSource,
            RegisteredAt = DateTimeOffset.Now,
        };

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
        config ??= PresetStore.LoadConfig();
        var registry = LoadRegistry(config);
        var entry = registry.Entries.FirstOrDefault(
            e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // Even without a registry entry, attempt to clean up by computed conventions.
        var aumid = entry?.Aumid ?? ComputeAumid(name, config);

        // Find shortcut path: from registry (recomputed), or by personality lookup.
        string? shortcut = null;
        if (entry?.DisplayName is not null)
            shortcut = GetShortcutPath(entry.DisplayName);
        else
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

        UnregisterAumid(aumid);

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
            var shortcutPath = entry?.ShortcutPath
                ?? (string.IsNullOrWhiteSpace(p.DisplayName) ? null : GetShortcutPath(p.DisplayName!));
            result.Add(new PersonalityInfo
            {
                Name = name,
                Aumid = aumid,
                DisplayName = p.DisplayName,
                Icon = p.Icon,
                InConfig = true,
                RegisteredInWindows = !string.IsNullOrEmpty(shortcutPath) && File.Exists(shortcutPath),
                ShortcutPath = shortcutPath,
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
                Icon = entry.IconSource,
                InConfig = false,
                RegisteredInWindows = File.Exists(entry.ShortcutPath),
                ShortcutPath = entry.ShortcutPath,
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
            // Never remove the built-in default through bulk ops; it would just
            // get auto-recreated on the next default toast.
            .Where(e => !e.Name.Equals(BuiltInDefaultName, StringComparison.OrdinalIgnoreCase))
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
            else if (!info.InConfig && info.RegisteredInWindows
                && !info.Name.Equals(BuiltInDefaultName, StringComparison.OrdinalIgnoreCase))
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
            // Never prune the built-in default; it's "stale" by design.
            if (info.Name.Equals(BuiltInDefaultName, StringComparison.OrdinalIgnoreCase))
                continue;
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

    /// <summary>
    /// Path to use as the Start Menu shortcut target. We deliberately avoid
    /// returning the generic <c>dotnet.exe</c> host: if every Palantir personality
    /// targeted dotnet.exe, the Compat notifier (and any other tool that maps
    /// shortcuts to AUMIDs by exe path) would conflate Palantir with every other
    /// .NET app, and our personality AUMID would be picked up as the "default"
    /// for any plain <c>dotnet run</c> invocation.
    /// </summary>
    public static string ResolveTargetExe()
    {
        var current = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? Assembly.GetEntryAssembly()?.Location
            ?? throw new InvalidOperationException("Cannot determine Palantir executable path.");

        var fileName = Path.GetFileName(current);
        if (fileName.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            // Running under the dotnet host (dotnet run / dotnet exec / dnx).
            // Substitute a Palantir-specific path so the shortcut target is
            // unique to this app.
            var entry = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entry))
            {
                // Prefer a sibling .exe (dotnet tool wrapper) when available.
                var dir = Path.GetDirectoryName(entry);
                var stem = Path.GetFileNameWithoutExtension(entry);
                if (dir is not null && stem is not null)
                {
                    var siblingExe = Path.Combine(dir, stem + ".exe");
                    if (File.Exists(siblingExe)) return siblingExe;
                }
                // Fall back to the .dll path itself: not directly launchable
                // by Windows shortcuts, but distinct from generic dotnet.exe so
                // Compat's exe-path matching won't pick up our shortcut.
                return entry;
            }
        }

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

    /// <summary>
    /// Register the stub COM activator under HKCU\Software\Classes\CLSID\{guid}
    /// so Windows accepts our shortcuts as valid toast sources. Idempotent and
    /// per-machine cheap (just a couple of registry keys).
    /// </summary>
    private static void EnsureToastActivatorRegistered(string targetExe)
    {
        if (!OperatingSystem.IsWindows()) return;

        var clsidKey = $@"Software\Classes\CLSID\{{{ToastActivatorClsid:D}}}";
        try
        {
            using var root = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(clsidKey);
            if (root is null) return;
            root.SetValue(null, "Palantir Toast Activator");
            using var local = root.CreateSubKey("LocalServer32");
            // The exe will only be invoked for cold-start (Action Center click
            // after process exit). For our in-process Activated event flow this
            // is unused, but Windows requires the key to exist + resolve.
            local?.SetValue(null, $"\"{targetExe}\"");
        }
        catch
        {
            // Best-effort: failure here may suppress toasts but shouldn't crash.
        }
    }

    /// <summary>
    /// Register the AUMID itself under HKCU\Software\Classes\AppUserModelId\&lt;aumid&gt;
    /// with DisplayName, IconUri, and CustomActivator pointing to our stub CLSID.
    /// This is what Windows actually consults to validate the AUMID for toast
    /// display — without it, Show() succeeds but no banner / Action Center entry
    /// is produced, even if the Start Menu shortcut has the right properties.
    /// </summary>
    private static void RegisterAumid(string aumid, string displayName, string iconPath)
    {
        if (!OperatingSystem.IsWindows()) return;

        var key = $@"Software\Classes\AppUserModelId\{aumid}";
        try
        {
            using var root = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(key);
            if (root is null) return;
            root.SetValue("DisplayName", displayName, Microsoft.Win32.RegistryValueKind.String);
            root.SetValue("IconUri", iconPath, Microsoft.Win32.RegistryValueKind.String);
            root.SetValue("CustomActivator",
                $"{{{ToastActivatorClsid:D}}}",
                Microsoft.Win32.RegistryValueKind.String);
            // Surface in Settings → Notifications. 1 = enabled by default.
            root.SetValue("ShowInSettings", 1, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch
        {
            // Best-effort.
        }
    }

    private static void UnregisterAumid(string aumid)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
                $@"Software\Classes\AppUserModelId\{aumid}", throwOnMissingSubKey: false);
        }
        catch { }
    }
}
