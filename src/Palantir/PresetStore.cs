using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Palantir;

/// <summary>
/// Root configuration file model. Extensible — add new top-level properties
/// alongside <see cref="Presets"/> for future configuration needs.
/// </summary>
public sealed class PalantirConfig
{
    [JsonPropertyName("presets")]
    public Dictionary<string, ToastOptions> Presets { get; set; } = new();
}

/// <summary>
/// Manages preset storage, resolution, and merging.
/// Config file location (first match wins):
///   1. PALANTIR_CONFIG_PATH environment variable (directory)
///   2. $XDG_CONFIG_HOME/palantir
///   3. %APPDATA%\Palantir
/// </summary>
public static class PresetStore
{
    private const string ConfigFileName = "palantir.json";

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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly Dictionary<string, ToastOptions> BuiltInPresets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["alarm"] = new()
            {
                Scenario = "alarm", Audio = "alarm", Loop = true, Duration = "long",
            },
            ["reminder"] = new()
            {
                Scenario = "reminder", Audio = "reminder", Duration = "long",
            },
            ["call"] = new()
            {
                Scenario = "incomingCall", Audio = "call", Loop = true, Duration = "long",
            },
        };

    // ── Config path resolution ──────────────────────────────────────

    /// <summary>
    /// Resolve the configuration directory using the precedence chain.
    /// </summary>
    public static string GetConfigDirectory()
    {
        // 1. PALANTIR_CONFIG_PATH
        var envPath = Environment.GetEnvironmentVariable("PALANTIR_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
            return envPath;

        // 2. XDG_CONFIG_HOME/palantir
        var xdgHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgHome))
            return Path.Combine(xdgHome, "Palantir");

        // 3. %APPDATA%\Palantir
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Palantir");
    }

    /// <summary>Full path to the config file.</summary>
    public static string GetConfigFilePath() => Path.Combine(GetConfigDirectory(), ConfigFileName);

    // ── Load / Save ─────────────────────────────────────────────────

    public static PalantirConfig LoadConfig()
    {
        var path = GetConfigFilePath();
        if (!File.Exists(path))
            return new PalantirConfig();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PalantirConfig>(json, ReadOptions)
               ?? new PalantirConfig();
    }

    public static void SaveConfig(PalantirConfig config)
    {
        var path = GetConfigFilePath();
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Serialize with clean preset representation (no empty arrays)
        var root = new JsonObject();
        var presetsNode = new JsonObject();

        foreach (var (name, opts) in config.Presets)
            presetsNode[name] = CleanSerialize(opts);

        root["presets"] = presetsNode;

        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    // ── Preset CRUD ─────────────────────────────────────────────────

    /// <summary>
    /// Look up a preset by name. User presets shadow built-in presets.
    /// </summary>
    public static ToastOptions? GetPreset(string name)
    {
        var config = LoadConfig();

        // User presets take precedence
        var key = config.Presets.Keys
            .FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (key is not null)
            return config.Presets[key];

        if (BuiltInPresets.TryGetValue(name, out var builtIn))
            return builtIn;

        return null;
    }

    /// <summary>
    /// Save a preset to the user config file.
    /// </summary>
    public static void SavePreset(string name, ToastOptions preset)
    {
        var config = LoadConfig();

        // Remove any existing key with a different casing
        var existingKey = config.Presets.Keys
            .FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existingKey is not null)
            config.Presets.Remove(existingKey);

        config.Presets[name] = preset;
        SaveConfig(config);
    }

    /// <summary>
    /// Delete a user preset. Returns false if the preset doesn't exist in user config.
    /// </summary>
    public static bool DeletePreset(string name)
    {
        var config = LoadConfig();

        var key = config.Presets.Keys
            .FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (key is null)
            return false;

        config.Presets.Remove(key);
        SaveConfig(config);
        return true;
    }

    public static bool IsBuiltIn(string name) =>
        BuiltInPresets.ContainsKey(name);

    public static IReadOnlyDictionary<string, ToastOptions> GetBuiltInPresets() =>
        BuiltInPresets;

    public static Dictionary<string, ToastOptions> GetUserPresets() =>
        LoadConfig().Presets;

    // ── Merge ───────────────────────────────────────────────────────

    /// <summary>
    /// Merge a preset into the target options.
    /// Only applies values that are non-null/non-default in the preset
    /// AND not in the <paramref name="explicitOptions"/> set.
    /// </summary>
    public static void MergePreset(
        ToastOptions target,
        ToastOptions preset,
        ISet<string>? explicitOptions = null)
    {
        explicitOptions ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── String? fields ──────────────────────────────────────────
        if (!explicitOptions.Contains("title") && preset.Title is not null)
            target.Title ??= preset.Title;
        if (!explicitOptions.Contains("message") && preset.Message is not null)
            target.Message ??= preset.Message;
        if (!explicitOptions.Contains("body") && preset.Body is not null)
            target.Body ??= preset.Body;
        if (!explicitOptions.Contains("attribution") && preset.Attribution is not null)
            target.Attribution ??= preset.Attribution;
        if (!explicitOptions.Contains("image") && preset.Image is not null)
            target.Image ??= preset.Image;
        if (!explicitOptions.Contains("heroImage") && preset.HeroImage is not null)
            target.HeroImage ??= preset.HeroImage;
        if (!explicitOptions.Contains("inlineImage") && preset.InlineImage is not null)
            target.InlineImage ??= preset.InlineImage;
        if (!explicitOptions.Contains("audio") && preset.Audio is not null)
            target.Audio ??= preset.Audio;
        if (!explicitOptions.Contains("duration") && preset.Duration is not null)
            target.Duration ??= preset.Duration;
        if (!explicitOptions.Contains("scenario") && preset.Scenario is not null)
            target.Scenario ??= preset.Scenario;
        if (!explicitOptions.Contains("timestamp") && preset.Timestamp is not null)
            target.Timestamp ??= preset.Timestamp;
        if (!explicitOptions.Contains("progressTitle") && preset.ProgressTitle is not null)
            target.ProgressTitle ??= preset.ProgressTitle;
        if (!explicitOptions.Contains("progressValue") && preset.ProgressValue is not null)
            target.ProgressValue ??= preset.ProgressValue;
        if (!explicitOptions.Contains("progressValueString") && preset.ProgressValueString is not null)
            target.ProgressValueString ??= preset.ProgressValueString;
        if (!explicitOptions.Contains("progressStatus") && preset.ProgressStatus is not null)
            target.ProgressStatus ??= preset.ProgressStatus;
        if (!explicitOptions.Contains("tag") && preset.Tag is not null)
            target.Tag ??= preset.Tag;
        if (!explicitOptions.Contains("group") && preset.Group is not null)
            target.Group ??= preset.Group;
        if (!explicitOptions.Contains("headerId") && preset.HeaderId is not null)
            target.HeaderId ??= preset.HeaderId;
        if (!explicitOptions.Contains("headerTitle") && preset.HeaderTitle is not null)
            target.HeaderTitle ??= preset.HeaderTitle;
        if (!explicitOptions.Contains("headerArguments") && preset.HeaderArguments is not null)
            target.HeaderArguments ??= preset.HeaderArguments;
        if (!explicitOptions.Contains("launch") && preset.LaunchUri is not null)
            target.LaunchUri ??= preset.LaunchUri;
        if (!explicitOptions.Contains("onClick") && preset.OnClickCommand is not null)
            target.OnClickCommand ??= preset.OnClickCommand;

        // ── Bool fields (preset can only turn on, not off) ──────────
        if (!explicitOptions.Contains("cropCircle") && preset.CropCircle)
            target.CropCircle = true;
        if (!explicitOptions.Contains("silent") && preset.Silent)
            target.Silent = true;
        if (!explicitOptions.Contains("loop") && preset.Loop)
            target.Loop = true;
        if (!explicitOptions.Contains("wait") && preset.Wait)
            target.Wait = true;

        // ── Int? fields ─────────────────────────────────────────────
        if (!explicitOptions.Contains("expiration") && preset.Expiration.HasValue)
            target.Expiration ??= preset.Expiration;
        if (!explicitOptions.Contains("timeout") && preset.Timeout.HasValue)
            target.Timeout ??= preset.Timeout;

        // ── Array fields (only apply if preset has values and target is empty) ──
        if (!explicitOptions.Contains("buttons") && preset.Buttons is { Length: > 0 }
            && target.Buttons is { Length: 0 })
            target.Buttons = preset.Buttons;
        if (!explicitOptions.Contains("inputs") && preset.Inputs is { Length: > 0 }
            && target.Inputs is { Length: 0 })
            target.Inputs = preset.Inputs;
        if (!explicitOptions.Contains("selections") && preset.Selections is { Length: > 0 }
            && target.Selections is { Length: 0 })
            target.Selections = preset.Selections;
    }

    // ── Display helpers ─────────────────────────────────────────────

    /// <summary>
    /// Format a one-line summary of a preset's key settings.
    /// </summary>
    public static string FormatSummary(ToastOptions opts)
    {
        var parts = new List<string>();
        if (opts.Scenario is not null) parts.Add($"scenario={opts.Scenario}");
        if (opts.Audio is not null) parts.Add($"audio={opts.Audio}");
        if (opts.Loop) parts.Add("loop");
        if (opts.Silent) parts.Add("silent");
        if (opts.Duration is not null) parts.Add($"duration={opts.Duration}");
        if (opts.Title is not null) parts.Add($"title=\"{opts.Title}\"");
        if (opts.Message is not null) parts.Add($"message=\"{opts.Message}\"");
        if (opts.Attribution is not null) parts.Add($"attribution=\"{opts.Attribution}\"");
        if (opts.Image is not null) parts.Add($"image={opts.Image}");
        if (opts.HeroImage is not null) parts.Add($"hero-image={opts.HeroImage}");
        if (opts.LaunchUri is not null) parts.Add($"launch={opts.LaunchUri}");
        if (opts.Buttons is { Length: > 0 }) parts.Add($"buttons={opts.Buttons.Length}");
        if (opts.Inputs is { Length: > 0 }) parts.Add($"inputs={opts.Inputs.Length}");
        if (opts.Expiration.HasValue) parts.Add($"expiration={opts.Expiration}min");
        if (opts.ProgressValue is not null) parts.Add($"progress={opts.ProgressValue}");
        if (opts.HeaderId is not null) parts.Add($"header={opts.HeaderId}");
        if (opts.CropCircle) parts.Add("crop-circle");
        if (opts.Wait) parts.Add("wait");
        if (opts.OnClickCommand is not null) parts.Add($"on-click=\"{opts.OnClickCommand}\"");
        return parts.Count > 0 ? string.Join(", ", parts) : "(empty)";
    }

    /// <summary>
    /// Serialize a preset to a clean JSON string (no empty arrays, no defaults).
    /// </summary>
    public static string SerializePresetJson(ToastOptions opts)
    {
        var node = CleanSerialize(opts);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Deserialize a JSON string to a ToastOptions preset.
    /// </summary>
    public static ToastOptions DeserializePreset(string json) =>
        JsonSerializer.Deserialize<ToastOptions>(json, ReadOptions) ?? new ToastOptions();

    // ── Private helpers ─────────────────────────────────────────────

    /// <summary>
    /// Serialize a ToastOptions to a JsonNode, stripping empty arrays and
    /// fields that should not appear in presets (preset, dryRun).
    /// </summary>
    private static JsonNode CleanSerialize(ToastOptions opts)
    {
        var node = JsonSerializer.SerializeToNode(opts, WriteOptions);
        if (node is JsonObject obj)
        {
            // Remove empty arrays (they serialize as [] because the default is [],
            // but default(string[]) is null, so WhenWritingDefault doesn't catch them)
            var keysToRemove = obj
                .Where(kvp => kvp.Value is JsonArray { Count: 0 })
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in keysToRemove)
                obj.Remove(key);

            // Remove fields that shouldn't be persisted in presets
            obj.Remove("preset");
            obj.Remove("dryRun");
        }
        return node!;
    }
}
