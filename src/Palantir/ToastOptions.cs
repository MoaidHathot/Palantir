using System.Text.Json.Serialization;

namespace Palantir;

/// <summary>
/// One line of toast text with optional styling.
/// </summary>
public sealed class TextLine
{
    /// <summary>Text content.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Friendly alias (header, large, normal, small, dim) or raw schema value
    /// (caption, captionSubtle, body, bodySubtle, base, baseSubtle, subtitle,
    /// subtitleSubtle, title, titleSubtle, titleNumeral, subheader,
    /// subheaderSubtle, subheaderNumeral, header, headerSubtle, headerNumeral).
    /// Null = unstyled.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>"left", "center", or "right". Null = unspecified.</summary>
    public string? Align { get; set; }
}

/// <summary>
/// Anchor location inside the toast XML where a fragment is injected.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum XmlAnchor
{
    /// <summary>Append children inside &lt;binding&gt; (default).</summary>
    Binding,

    /// <summary>Append children inside &lt;actions&gt; (creates the element if absent).</summary>
    Actions,

    /// <summary>Append children inside &lt;toast&gt;.</summary>
    Toast,
}

/// <summary>
/// A raw XML fragment to inject at a specific anchor.
/// </summary>
public sealed class XmlFragment
{
    /// <summary>Raw XML fragment string. May contain one or more sibling elements.</summary>
    public string Fragment { get; set; } = "";

    /// <summary>Anchor element to inject inside.</summary>
    public XmlAnchor Anchor { get; set; } = XmlAnchor.Binding;
}

/// <summary>
/// Represents all configurable options for a Windows Toast Notification.
/// </summary>
public sealed class ToastOptions
{
    /// <summary>Toast title text (first line, bold).</summary>
    public string? Title { get; set; }

    /// <summary>Toast body/message text (second line).</summary>
    public string? Message { get; set; }

    /// <summary>Third line of text content.</summary>
    public string? Body { get; set; }

    /// <summary>Attribution text shown at the bottom (e.g. "Via Palantir").</summary>
    public string? Attribution { get; set; }

    // ── Images ──────────────────────────────────────────────────────

    /// <summary>App logo override image (file path or http/https URL).</summary>
    public string? Image { get; set; }

    /// <summary>Crop the app logo as a circle instead of square.</summary>
    public bool CropCircle { get; set; }

    /// <summary>Hero image displayed prominently at the top (file path or URL).</summary>
    public string? HeroImage { get; set; }

    /// <summary>Inline image shown in the toast body (file path or URL).</summary>
    public string? InlineImage { get; set; }

    // ── Buttons & Inputs ────────────────────────────────────────────

    /// <summary>
    /// Buttons to add. Formats:
    ///   "Label" or "Label;dismiss" — dismiss button
    ///   "Label;submit" — foreground activation (captures user input)
    ///   "Label;uri" — protocol activation (opens URI)
    ///   "label=X,action=submit" — structured key-value format
    /// </summary>
    public string[] Buttons { get; set; } = [];

    /// <summary>
    /// Text input boxes. Each entry is either "id" or "id;placeholder".
    /// </summary>
    public string[] Inputs { get; set; } = [];

    /// <summary>
    /// Selection (combo box) inputs. Format: "id;Option A,Option B,Option C".
    /// </summary>
    public string[] Selections { get; set; } = [];

    // ── Audio ───────────────────────────────────────────────────────

    /// <summary>
    /// Audio to play. Named sound (e.g. "default", "im", "mail", "reminder", "sms",
    /// "alarm", "alarm2"–"alarm10", "call", "call2"–"call10") or a file path.
    /// </summary>
    public string? Audio { get; set; }

    /// <summary>Suppress all audio (silent notification).</summary>
    public bool Silent { get; set; }

    /// <summary>Loop the audio sound.</summary>
    public bool Loop { get; set; }

    // ── Behavior ────────────────────────────────────────────────────

    /// <summary>
    /// Toast duration: "short" (default, ~5s) or "long" (~25s).
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Toast scenario: "default", "alarm", "reminder", or "incomingCall".
    /// </summary>
    public string? Scenario { get; set; }

    /// <summary>Expiration time in minutes from now.</summary>
    public int? Expiration { get; set; }

    /// <summary>Custom timestamp (ISO 8601).</summary>
    public string? Timestamp { get; set; }

    // ── Progress Bar ────────────────────────────────────────────────

    /// <summary>Progress bar title.</summary>
    public string? ProgressTitle { get; set; }

    /// <summary>Progress bar value (0.0 to 1.0) or "indeterminate".</summary>
    public string? ProgressValue { get; set; }

    /// <summary>Progress bar value string override (e.g. "3/10 songs").</summary>
    public string? ProgressValueString { get; set; }

    /// <summary>Progress bar status text (e.g. "Downloading...").</summary>
    public string? ProgressStatus { get; set; }

    // ── Identity ────────────────────────────────────────────────────

    /// <summary>Toast tag for identifying/updating toasts.</summary>
    public string? Tag { get; set; }

    /// <summary>Toast group for organizing/updating toasts.</summary>
    public string? Group { get; set; }

    // ── Header ──────────────────────────────────────────────────────

    /// <summary>Header ID for grouping related toasts in Action Center.</summary>
    public string? HeaderId { get; set; }

    /// <summary>Header display title.</summary>
    public string? HeaderTitle { get; set; }

    /// <summary>Header activation arguments.</summary>
    public string? HeaderArguments { get; set; }

    // ── Launch ──────────────────────────────────────────────────────

    /// <summary>Launch URI when the toast body is clicked.</summary>
    public string? LaunchUri { get; set; }

    /// <summary>Shell command to execute when the toast is activated (implies --wait).</summary>
    public string? OnClickCommand { get; set; }

    // ── Behavior Flags ──────────────────────────────────────────────

    /// <summary>Preset to apply (alarm, reminder, call).</summary>
    public string? Preset { get; set; }

    /// <summary>Block until the toast is dismissed or activated.</summary>
    public bool Wait { get; set; }

    /// <summary>Timeout in seconds for --wait (null = wait indefinitely).</summary>
    public int? Timeout { get; set; }

    /// <summary>Output the toast XML without displaying it.</summary>
    public bool DryRun { get; set; }

    // ── Styling (per-line) ──────────────────────────────────────────

    /// <summary>
    /// Style for the title line. Friendly alias (header/large/normal/small/dim)
    /// or raw toast schema value (e.g. baseSubtle, titleNumeral). Null = unstyled.
    /// </summary>
    public string? TitleStyle { get; set; }

    /// <summary>Alignment for the title line: "left", "center", or "right".</summary>
    public string? TitleAlign { get; set; }

    /// <summary>Style for the message line. See <see cref="TitleStyle"/> for accepted values.</summary>
    public string? MessageStyle { get; set; }

    /// <summary>Alignment for the message line.</summary>
    public string? MessageAlign { get; set; }

    /// <summary>Style for the body line. See <see cref="TitleStyle"/> for accepted values.</summary>
    public string? BodyStyle { get; set; }

    /// <summary>Alignment for the body line.</summary>
    public string? BodyAlign { get; set; }

    // ── Extra Text Lines ────────────────────────────────────────────

    /// <summary>
    /// Additional &lt;text&gt; lines beyond Title/Message/Body, with optional per-line styling.
    /// </summary>
    public List<TextLine> ExtraTexts { get; set; } = [];

    // ── Group / Subgroup Layout ─────────────────────────────────────

    /// <summary>
    /// Multi-row, multi-column rich layout. Outer list = rows (each becomes a &lt;group&gt;);
    /// inner list = columns within that row (each becomes a &lt;subgroup&gt; with one styled text).
    /// </summary>
    public List<List<TextLine>> Groups { get; set; } = [];

    // ── Escape Hatches ──────────────────────────────────────────────

    /// <summary>
    /// Verbatim &lt;text&gt; XML elements appended after structured text lines.
    /// Each entry must parse as a single &lt;text&gt; element when validation is enabled.
    /// </summary>
    public List<string> RawTextElements { get; set; } = [];

    /// <summary>
    /// Arbitrary XML fragments to inject at chosen anchors inside the toast XML.
    /// </summary>
    public List<XmlFragment> XmlFragments { get; set; } = [];

    /// <summary>
    /// When true (library default), validate raw XML fragments before sending.
    /// CLI default is false for performance; users opt in via --validate-xml.
    /// </summary>
    public bool ValidateXml { get; set; } = true;

    /// <summary>
    /// When true, expand emoji shortcodes (e.g. ":check:" → ✅) in all text fields.
    /// Off by default. Unknown shortcodes are left as literal text.
    /// </summary>
    public bool ExpandShortcodes { get; set; }

    // ── Personality (toast app identity) ────────────────────────────

    /// <summary>
    /// Personality name (looked up in palantir.json's <c>personalities</c> section).
    /// Determines the corner icon and app name Windows shows on the toast.
    /// Auto-registers the personality on first use.
    /// </summary>
    public string? Personality { get; set; }

    /// <summary>
    /// One-off override for the corner app name. Implies an ad-hoc personality
    /// (registered with a derived AUMID).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// One-off override for the corner app icon (file path or HTTP URL,
    /// .ico/.png/.jpg). Implies an ad-hoc personality.
    /// </summary>
    public string? AppIcon { get; set; }
}
