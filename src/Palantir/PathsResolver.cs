namespace Palantir;

/// <summary>
/// Resolves cache / icon / image / registry paths from config + env vars + defaults.
/// All path values support token expansion: <c>${ENV_VAR}</c>, <c>${PALANTIR_CONFIG}</c>,
/// <c>${PALANTIR_CACHE}</c>, and a leading <c>~</c> for the user profile.
/// </summary>
public static class PathsResolver
{
    /// <summary>Cache root.</summary>
    /// <remarks>
    /// Resolution: <c>paths.cache</c> → <c>PALANTIR_CACHE_PATH</c> →
    /// <c>XDG_CACHE_HOME</c>/palantir → <c>XDG_CONFIG_HOME</c>/palantir/cache →
    /// <c>%LocalAppData%\Palantir\cache</c>.
    /// </remarks>
    public static string GetCacheDirectory(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Cache))
            return Expand(config.Paths.Cache, config);

        var env = Environment.GetEnvironmentVariable("PALANTIR_CACHE_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env, config);

        var xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(xdgCache))
            return Path.Combine(Expand(xdgCache, config), "palantir");

        var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfig))
            return Path.Combine(Expand(xdgConfig, config), "palantir", "cache");

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Palantir", "cache");
    }

    /// <summary>Icon cache directory. Default: &lt;cache&gt;/icons.</summary>
    public static string GetIconsDirectory(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Icons))
            return Expand(config.Paths.Icons, config);

        var env = Environment.GetEnvironmentVariable("PALANTIR_ICONS_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env, config);

        return Path.Combine(GetCacheDirectory(config), "icons");
    }

    /// <summary>Image cache directory. Default: &lt;cache&gt;/images.</summary>
    public static string GetImagesDirectory(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Images))
            return Expand(config.Paths.Images, config);

        var env = Environment.GetEnvironmentVariable("PALANTIR_IMAGES_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env, config);

        return Path.Combine(GetCacheDirectory(config), "images");
    }

    /// <summary>
    /// Personality registry file. State, not config — kept out of the config
    /// directory so config can be portable / synced.
    /// </summary>
    /// <remarks>
    /// Resolution: <c>paths.registry</c> → <c>PALANTIR_REGISTRY_PATH</c> →
    /// <c>XDG_STATE_HOME</c>/palantir/registry.json →
    /// <c>%LocalAppData%\Palantir\state\registry.json</c>.
    /// </remarks>
    public static string GetRegistryFilePath(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Registry))
            return Expand(config.Paths.Registry, config);

        var env = Environment.GetEnvironmentVariable("PALANTIR_REGISTRY_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env, config);

        var xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        if (!string.IsNullOrWhiteSpace(xdgState))
            return Path.Combine(Expand(xdgState, config), "palantir", "registry.json");

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Palantir", "state", "registry.json");
    }

    /// <summary>Ensure a directory exists, returning the path for chaining.</summary>
    public static string EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Expand path tokens in <paramref name="path"/>:
    /// <list type="bullet">
    ///   <item><c>${ENV_VAR}</c> from the process environment</item>
    ///   <item><c>${PALANTIR_CONFIG}</c> → resolved config directory</item>
    ///   <item><c>${PALANTIR_CACHE}</c> → resolved cache root (avoid in cache config to prevent loops)</item>
    ///   <item>leading <c>~</c> → user profile</item>
    /// </list>
    /// </summary>
    public static string Expand(string path, PalantirConfig? config = null)
    {
        if (string.IsNullOrEmpty(path)) return path;

        var expanded = ExpandTokens(path, config);
        // Standard env var expansion (handles %VAR% on Windows too).
        expanded = Environment.ExpandEnvironmentVariables(expanded);

        if (expanded.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = Path.Combine(home, expanded[1..].TrimStart('/', '\\'));
        }

        return Path.GetFullPath(expanded);
    }

    /// <summary>
    /// Like <see cref="Expand"/> but does not call <c>Path.GetFullPath</c>.
    /// Use for non-path string fields that still benefit from token substitution.
    /// </summary>
    public static string ExpandValue(string value, PalantirConfig? config = null)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var expanded = ExpandTokens(value, config);
        return Environment.ExpandEnvironmentVariables(expanded);
    }

    private static string ExpandTokens(string input, PalantirConfig? config)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains("${"))
            return input;

        var result = new System.Text.StringBuilder(input.Length);
        var i = 0;
        while (i < input.Length)
        {
            if (i + 1 < input.Length && input[i] == '$' && input[i + 1] == '{')
            {
                var end = input.IndexOf('}', i + 2);
                if (end > i + 2)
                {
                    var token = input.Substring(i + 2, end - i - 2);
                    var value = ResolveToken(token, config);
                    if (value is not null)
                    {
                        result.Append(value);
                        i = end + 1;
                        continue;
                    }
                }
            }
            result.Append(input[i]);
            i++;
        }
        return result.ToString();
    }

    private static string? ResolveToken(string token, PalantirConfig? config)
    {
        // Avoid recursion: PALANTIR_CONFIG / PALANTIR_CACHE tokens cannot resolve
        // a path that itself uses them (we'd loop on PALANTIR_CACHE inside paths.cache).
        return token switch
        {
            "PALANTIR_CONFIG" => PresetStore.GetConfigDirectory(),
            "PALANTIR_CACHE"  => GetCacheDirectoryRaw(config),
            _ => Environment.GetEnvironmentVariable(token),
        };
    }

    /// <summary>
    /// Cache directory resolution that does NOT recurse via <see cref="ExpandTokens"/>
    /// — used internally to support <c>${PALANTIR_CACHE}</c> tokens elsewhere.
    /// </summary>
    private static string GetCacheDirectoryRaw(PalantirConfig? config)
    {
        config ??= PresetStore.LoadConfig();
        // Skip the paths.cache step so ${PALANTIR_CACHE} can't recurse into
        // a config that uses ${PALANTIR_CACHE}.
        var env = Environment.GetEnvironmentVariable("PALANTIR_CACHE_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Environment.ExpandEnvironmentVariables(env);

        var xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(xdgCache))
            return Path.Combine(Environment.ExpandEnvironmentVariables(xdgCache), "palantir");

        var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfig))
            return Path.Combine(Environment.ExpandEnvironmentVariables(xdgConfig), "palantir", "cache");

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Palantir", "cache");
    }
}
