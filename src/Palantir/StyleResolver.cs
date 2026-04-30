namespace Palantir;

/// <summary>
/// Resolves friendly style/align aliases to the toast schema's hint-style / hint-align values.
/// Raw schema values are passed through unchanged. Unknown values throw with a clear message.
/// </summary>
internal static class StyleResolver
{
    private static readonly Dictionary<string, string> StyleAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["header"] = "header",
            ["large"]  = "title",
            ["normal"] = "base",
            ["small"]  = "caption",
            ["dim"]    = "baseSubtle",
        };

    /// <summary>The full set of toast schema hint-style values, accepted as-is.</summary>
    public static readonly HashSet<string> ValidSchemaStyles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "caption",   "captionSubtle",
            "body",      "bodySubtle",
            "base",      "baseSubtle",
            "subtitle",  "subtitleSubtle",
            "title",     "titleSubtle",  "titleNumeral",
            "subheader", "subheaderSubtle", "subheaderNumeral",
            "header",    "headerSubtle", "headerNumeral",
        };

    public static readonly HashSet<string> ValidAligns =
        new(StringComparer.OrdinalIgnoreCase) { "left", "center", "right" };

    /// <summary>
    /// Resolve a style value (alias or raw schema). Returns the schema value to emit,
    /// or null if input is null/whitespace.
    /// </summary>
    public static string? ResolveStyle(string? input, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();

        if (StyleAliases.TryGetValue(s, out var mapped))
            return mapped;

        if (ValidSchemaStyles.Contains(s))
        {
            // Normalize casing to the canonical schema casing for consistency.
            return ValidSchemaStyles.First(v => v.Equals(s, StringComparison.OrdinalIgnoreCase));
        }

        throw new ArgumentException(
            $"Invalid style \"{input}\" for {fieldLabel}. " +
            $"Friendly aliases: {string.Join(", ", StyleAliases.Keys)}. " +
            $"Raw schema values: {string.Join(", ", ValidSchemaStyles)}.");
    }

    /// <summary>
    /// Resolve an alignment value. Returns lowercase schema value or null.
    /// </summary>
    public static string? ResolveAlign(string? input, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim().ToLowerInvariant();

        if (ValidAligns.Contains(s)) return s;

        throw new ArgumentException(
            $"Invalid alignment \"{input}\" for {fieldLabel}. " +
            $"Valid: {string.Join(", ", ValidAligns)}.");
    }
}
