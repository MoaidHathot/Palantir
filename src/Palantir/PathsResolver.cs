namespace Palantir;

/// <summary>
/// Resolves cache / icon / image / registry paths from config + env vars + defaults.
/// Resolution order per key: explicit <c>paths.X</c> in palantir.json
/// → <c>PALANTIR_X_PATH</c> env var → built-in default derived from cache root.
/// </summary>
public static class PathsResolver
{
    /// <summary>Cache root. Default: <c>%LocalAppData%\Palantir\cache</c> or <c>$XDG_CACHE_HOME/palantir</c>.</summary>
    public static string GetCacheDirectory(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Cache))
            return Expand(config.Paths.Cache);

        var env = Environment.GetEnvironmentVariable("PALANTIR_CACHE_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env);

        var xdg = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(xdg))
            return Path.Combine(xdg, "Palantir");

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Palantir", "cache");
    }

    /// <summary>Icon cache directory. Default: &lt;cache&gt;/icons.</summary>
    public static string GetIconsDirectory(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Icons))
            return Expand(config.Paths.Icons);

        var env = Environment.GetEnvironmentVariable("PALANTIR_ICONS_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env);

        return Path.Combine(GetCacheDirectory(config), "icons");
    }

    /// <summary>Image cache directory. Default: &lt;cache&gt;/images.</summary>
    public static string GetImagesDirectory(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Images))
            return Expand(config.Paths.Images);

        var env = Environment.GetEnvironmentVariable("PALANTIR_IMAGES_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env);

        return Path.Combine(GetCacheDirectory(config), "images");
    }

    /// <summary>
    /// Personality registry file. Default: alongside config file as registry.json
    /// (state, not cache — surviving cache clear).
    /// </summary>
    public static string GetRegistryFilePath(PalantirConfig? config = null)
    {
        config ??= PresetStore.LoadConfig();
        if (!string.IsNullOrWhiteSpace(config.Paths?.Registry))
            return Expand(config.Paths.Registry);

        var env = Environment.GetEnvironmentVariable("PALANTIR_REGISTRY_PATH");
        if (!string.IsNullOrWhiteSpace(env))
            return Expand(env);

        return Path.Combine(PresetStore.GetConfigDirectory(), "registry.json");
    }

    /// <summary>Ensure a directory exists, returning the path for chaining.</summary>
    public static string EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    private static string Expand(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        if (expanded.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = Path.Combine(home, expanded[1..].TrimStart('/', '\\'));
        }
        return Path.GetFullPath(expanded);
    }
}
