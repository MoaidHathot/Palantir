using System.Text.RegularExpressions;

namespace Palantir;

/// <summary>
/// Opt-in emoji shortcode expander. Off by default; enable via
/// <see cref="ToastOptions.ExpandShortcodes"/> or the <c>--expand-shortcodes</c> flag.
/// Unknown shortcodes are left untouched.
/// </summary>
internal static class Shortcodes
{
    private static readonly Regex Pattern =
        new(@":[a-z0-9_+\-]+:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Built-in shortcode dictionary. Curated set of ~30 GitHub-style names.</summary>
    public static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [":check:"]        = "\u2705",        // ✅
            [":x:"]            = "\u274C",        // ❌
            [":warn:"]         = "\u26A0\uFE0F",  // ⚠️
            [":warning:"]      = "\u26A0\uFE0F",
            [":info:"]         = "\u2139\uFE0F",  // ℹ️
            [":question:"]     = "\u2753",        // ❓
            [":exclamation:"]  = "\u2757",        // ❗
            [":red_circle:"]   = "\uD83D\uDD34",  // 🔴
            [":green_circle:"] = "\uD83D\uDFE2",  // 🟢
            [":yellow_circle:"]= "\uD83D\uDFE1",  // 🟡
            [":blue_circle:"]  = "\uD83D\uDD35",  // 🔵
            [":white_circle:"] = "\u26AA",        // ⚪
            [":black_circle:"] = "\u26AB",        // ⚫
            [":bell:"]         = "\uD83D\uDD14",  // 🔔
            [":hourglass:"]    = "\u23F3",        // ⏳
            [":rocket:"]       = "\uD83D\uDE80",  // 🚀
            [":fire:"]         = "\uD83D\uDD25",  // 🔥
            [":sparkles:"]     = "\u2728",        // ✨
            [":lock:"]         = "\uD83D\uDD12",  // 🔒
            [":unlock:"]       = "\uD83D\uDD13",  // 🔓
            [":tada:"]         = "\uD83C\uDF89",  // 🎉
            [":wave:"]         = "\uD83D\uDC4B",  // 👋
            [":gear:"]         = "\u2699\uFE0F",  // ⚙️
            [":wrench:"]       = "\uD83D\uDD27",  // 🔧
            [":hammer:"]       = "\uD83D\uDD28",  // 🔨
            [":package:"]      = "\uD83D\uDCE6",  // 📦
            [":floppy_disk:"]  = "\uD83D\uDCBE",  // 💾
            [":zap:"]          = "\u26A1",        // ⚡
            [":bug:"]          = "\uD83D\uDC1B",  // 🐛
            [":mag:"]          = "\uD83D\uDD0D",  // 🔍
            [":eyes:"]         = "\uD83D\uDC40",  // 👀
            [":thumbsup:"]     = "\uD83D\uDC4D",  // 👍
            [":thumbsdown:"]   = "\uD83D\uDC4E",  // 👎
            [":heart:"]        = "\u2764\uFE0F",  // ❤️
            [":star:"]         = "\u2B50",        // ⭐
        };

    /// <summary>
    /// Expand all known shortcodes in <paramref name="input"/>.
    /// Returns the input unchanged if null/empty or no shortcodes found.
    /// </summary>
    public static string? Expand(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return Pattern.Replace(input, m =>
            Map.TryGetValue(m.Value, out var v) ? v : m.Value);
    }

    /// <summary>Expand only when <paramref name="enabled"/> is true.</summary>
    public static string? ExpandIf(string? input, bool enabled) =>
        enabled ? Expand(input) : input;
}
