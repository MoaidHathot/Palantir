namespace Palantir;

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
    /// Buttons to add. Each entry is either "Label" (dismiss) or "Label;uri" (protocol activation).
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
    /// Toast scenario: "default", "alarm", "reminder", "incomingCall", or "urgent".
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

    /// <summary>Application User Model ID (AUMID) for the toast source.</summary>
    public string? AppId { get; set; }

    /// <summary>Toast tag for identifying/updating toasts.</summary>
    public string? Tag { get; set; }

    /// <summary>Toast group for organizing/updating toasts.</summary>
    public string? Group { get; set; }

    // ── Launch ──────────────────────────────────────────────────────

    /// <summary>Launch URI when the toast body is clicked.</summary>
    public string? LaunchUri { get; set; }
}
