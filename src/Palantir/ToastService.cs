using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace Palantir;

/// <summary>
/// Builds and shows Windows Toast Notifications from <see cref="ToastOptions"/>.
/// </summary>
public static class ToastService
{
    internal const string DefaultProgressStatus = "In progress";
    private const int MaxSoundVariants = 10;

    internal static readonly string[] ValidDurations = ["short", "long"];
    internal static readonly string[] ValidScenarios = ["default", "alarm", "reminder", "incomingcall"];
    private static readonly Dictionary<string, string> SoundMap = BuildSoundMap();

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>
    /// Build and display a toast notification from the given options.
    /// </summary>
    public static void Show(ToastOptions options, Action<string>? onWarning = null)
    {
        var xml = BuildXml(options, onWarning);
        var toast = CreateNotification(xml);
        ConfigureNotification(toast, options);
        GetNotifier(options, onWarning)(toast);
    }

    /// <summary>
    /// Show a toast and wait for user interaction. Returns the result as JSON-serializable object.
    /// </summary>
    public static WaitResult ShowAndWait(
        ToastOptions options, int? timeoutSeconds = null, Action<string>? onWarning = null)
    {
        var xml = BuildXml(options, onWarning);
        var toast = CreateNotification(xml);
        ConfigureNotification(toast, options);

        var waitHandle = new ManualResetEventSlim(false);
        WaitResult? result = null;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            result ??= new WaitResult { Action = "cancelled" };
            waitHandle.Set();
        };

        // Ensure we have a tag for identification
        if (string.IsNullOrWhiteSpace(toast.Tag))
            toast.Tag = Guid.NewGuid().ToString("N")[..16];

        toast.Activated += (_, e) =>
        {
            var waitResult = new WaitResult { Action = "activated" };

            try
            {
                if (e is ToastActivatedEventArgs args)
                {
                    waitResult.Arguments = string.IsNullOrEmpty(args.Arguments)
                        ? null
                        : args.Arguments;

                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)
                        && args.UserInput?.Count > 0)
                    {
                        waitResult.UserInputs = new Dictionary<string, string>();
                        foreach (var kvp in args.UserInput)
                            waitResult.UserInputs[kvp.Key] = kvp.Value?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                // Ignore cast/access errors from WinRT projections
            }

            result ??= waitResult;
            waitHandle.Set();
        };

        toast.Dismissed += (_, e) =>
        {
            result ??= new WaitResult
            {
                Action = "dismissed",
                Reason = e.Reason.ToString(),
            };
            waitHandle.Set();
        };

        toast.Failed += (_, e) =>
        {
            result ??= new WaitResult
            {
                Action = "failed",
                Error = e.ErrorCode.Message,
            };
            waitHandle.Set();
        };

        GetNotifier(options, onWarning)(toast);

        waitHandle.Wait(timeoutSeconds.HasValue
            ? TimeSpan.FromSeconds(timeoutSeconds.Value)
            : Timeout.InfiniteTimeSpan);

        if (result is null)
            result = new WaitResult { Action = "timedOut" };

        return result;
    }

    /// <summary>
    /// Get the toast XML without showing it (dry-run mode).
    /// </summary>
    public static string GetXml(ToastOptions options, Action<string>? onWarning = null)
        => BuildXml(options, onWarning);

    /// <summary>
    /// Remove a specific toast from the notification history by tag.
    /// </summary>
    public static void Remove(string tag, string? group = null)
    {
        if (!string.IsNullOrWhiteSpace(group))
            ToastNotificationManagerCompat.History.Remove(tag, group);
        else
            ToastNotificationManagerCompat.History.Remove(tag);
    }

    /// <summary>
    /// Remove all toasts in a group from the notification history.
    /// </summary>
    public static void RemoveGroup(string group)
    {
        ToastNotificationManagerCompat.History.RemoveGroup(group);
    }

    /// <summary>
    /// Update an existing toast's data (e.g., progress bar values).
    /// </summary>
    public static NotificationUpdateResult Update(
        string tag,
        string? group,
        string? progressValue,
        string? progressValueString,
        string? progressStatus,
        string? progressTitle,
        uint? sequenceNumber)
    {
        var data = new NotificationData { SequenceNumber = sequenceNumber ?? 0 };

        if (progressValue is not null)
        {
            if (string.Equals(progressValue, "indeterminate", StringComparison.OrdinalIgnoreCase))
            {
                data.Values["progressValue"] = "";
            }
            else if (double.TryParse(progressValue, CultureInfo.InvariantCulture, out var parsed))
            {
                data.Values["progressValue"] = Math.Clamp(parsed, 0.0, 1.0)
                    .ToString(CultureInfo.InvariantCulture);
            }
        }

        if (progressValueString is not null)
            data.Values["progressValueStringOverride"] = progressValueString;

        if (progressStatus is not null)
            data.Values["progressStatus"] = progressStatus;

        if (progressTitle is not null)
            data.Values["progressTitle"] = progressTitle;

        return !string.IsNullOrWhiteSpace(group)
            ? ToastNotificationManagerCompat.CreateToastNotifier().Update(data, tag, group)
            : ToastNotificationManagerCompat.CreateToastNotifier().Update(data, tag);
    }

    /// <summary>
    /// Clears all toast notification history for this app.
    /// </summary>
    public static void ClearHistory()
    {
        ToastNotificationManagerCompat.History.Clear();
    }

    /// <summary>
    /// Get the list of active toast notifications for this app.
    /// </summary>
    public static IReadOnlyList<HistoryEntry> GetHistory()
    {
        var toasts = ToastNotificationManagerCompat.History.GetHistory();
        var entries = new List<HistoryEntry>();

        foreach (var toast in toasts)
        {
            var xmlString = toast.Content.GetXml();
            var texts = new List<string>();

            try
            {
                var doc = XDocument.Parse(xmlString);
                texts = doc.Descendants("text")
                    .Select(t => t.Value)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }
            catch
            {
                // If XML parsing fails, just skip text extraction
            }

            entries.Add(new HistoryEntry
            {
                Tag = string.IsNullOrEmpty(toast.Tag) ? null : toast.Tag,
                Group = string.IsNullOrEmpty(toast.Group) ? null : toast.Group,
                Texts = texts,
                ExpirationTime = toast.ExpirationTime,
            });
        }

        return entries;
    }

    // ── Private Helpers ─────────────────────────────────────────────

    private static ToastContentBuilder BuildToast(ToastOptions options, Action<string>? onWarning)
    {
        var builder = new ToastContentBuilder();
        var expand = options.ExpandShortcodes;

        // ── Text Content ────────────────────────────────────────────
        var title = Shortcodes.ExpandIf(options.Title, expand);
        var message = Shortcodes.ExpandIf(options.Message, expand);
        var body = Shortcodes.ExpandIf(options.Body, expand);
        var attribution = Shortcodes.ExpandIf(options.Attribution, expand);

        if (!string.IsNullOrWhiteSpace(title))
            builder.AddText(title);

        if (!string.IsNullOrWhiteSpace(message))
            builder.AddText(message);

        if (!string.IsNullOrWhiteSpace(body))
            builder.AddText(body);

        if (!string.IsNullOrWhiteSpace(attribution))
            builder.AddAttributionText(attribution);

        // ── Images ──────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Image))
        {
            var uri = ResolveImageUri(options.Image, onWarning);
            var crop = options.CropCircle
                ? ToastGenericAppLogoCrop.Circle
                : ToastGenericAppLogoCrop.Default;
            builder.AddAppLogoOverride(uri, crop);
        }

        if (!string.IsNullOrWhiteSpace(options.HeroImage))
        {
            var uri = ResolveImageUri(options.HeroImage, onWarning);
            builder.AddHeroImage(uri);
        }

        if (!string.IsNullOrWhiteSpace(options.InlineImage))
        {
            var uri = ResolveImageUri(options.InlineImage, onWarning);
            builder.AddInlineImage(uri);
        }

        // ── Buttons ─────────────────────────────────────────────────
        foreach (var buttonSpec in options.Buttons)
        {
            AddButton(builder, buttonSpec, onWarning, expand);
        }

        // ── Text Inputs ─────────────────────────────────────────────
        foreach (var inputSpec in options.Inputs)
        {
            var parts = inputSpec.Split(';', 2);
            var id = parts[0].Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                onWarning?.Invoke($"Warning: Ignoring input with empty id: \"{inputSpec}\"");
                continue;
            }

            var placeholder = parts.Length == 2
                ? Shortcodes.ExpandIf(parts[1].Trim(), expand)
                : null;
            builder.AddInputTextBox(id, placeholder);
        }

        // ── Selection Inputs ────────────────────────────────────────
        foreach (var selectionSpec in options.Selections)
        {
            var parts = selectionSpec.Split(';', 2);
            if (parts.Length < 2)
            {
                onWarning?.Invoke(
                    $"Warning: Ignoring malformed selection " +
                    $"(expected \"id;Option A,Option B,...\"): \"{selectionSpec}\"");
                continue;
            }

            var id = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                onWarning?.Invoke($"Warning: Ignoring selection with empty id: \"{selectionSpec}\"");
                continue;
            }

            var choices = parts[1].Split(',');
            var selectionBox = new ToastSelectionBox(id);
            foreach (var choice in choices)
            {
                var trimmed = Shortcodes.ExpandIf(choice.Trim(), expand);
                if (!string.IsNullOrEmpty(trimmed))
                    selectionBox.Items.Add(new ToastSelectionBoxItem(trimmed, trimmed));
            }

            if (selectionBox.Items.Count == 0)
            {
                onWarning?.Invoke($"Warning: Ignoring selection \"{id}\" with no valid choices.");
                continue;
            }

            builder.AddToastInput(selectionBox);
        }

        // ── Audio ───────────────────────────────────────────────────
        if (options.Silent)
        {
            builder.AddAudio(null, silent: true);
        }
        else if (!string.IsNullOrWhiteSpace(options.Audio))
        {
            var audioUri = ResolveAudioUri(options.Audio);
            builder.AddAudio(audioUri, loop: options.Loop);
        }
        else if (options.Loop)
        {
            builder.AddAudio(null, loop: true);
        }

        // ── Duration ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Duration))
        {
            var duration = options.Duration.ToLowerInvariant();
            if (!ValidDurations.Contains(duration))
            {
                onWarning?.Invoke(
                    $"Warning: Invalid duration \"{options.Duration}\". " +
                    $"Valid values: {string.Join(", ", ValidDurations)}. Defaulting to \"short\".");
            }

            builder.SetToastDuration(duration switch
            {
                "long" => ToastDuration.Long,
                _ => ToastDuration.Short,
            });
        }

        // ── Scenario ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Scenario))
        {
            var scenario = options.Scenario.ToLowerInvariant();
            if (!ValidScenarios.Contains(scenario))
            {
                onWarning?.Invoke(
                    $"Warning: Invalid scenario \"{options.Scenario}\". " +
                    $"Valid values: {string.Join(", ", ValidScenarios)}. Defaulting to \"default\".");
            }

            builder.SetToastScenario(scenario switch
            {
                "alarm" => ToastScenario.Alarm,
                "reminder" => ToastScenario.Reminder,
                "incomingcall" => ToastScenario.IncomingCall,
                _ => ToastScenario.Default,
            });
        }

        // ── Header ──────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.HeaderId))
        {
            var headerTitle = Shortcodes.ExpandIf(options.HeaderTitle, expand);
            builder.AddHeader(
                options.HeaderId,
                headerTitle ?? options.HeaderId,
                options.HeaderArguments ?? "");
        }

        // ── Progress Bar ────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.ProgressValue) ||
            !string.IsNullOrWhiteSpace(options.ProgressTitle) ||
            !string.IsNullOrWhiteSpace(options.ProgressStatus))
        {
            var isIndeterminate = string.Equals(
                options.ProgressValue, "indeterminate", StringComparison.OrdinalIgnoreCase);
            double? progressValue = null;

            if (!isIndeterminate && !string.IsNullOrWhiteSpace(options.ProgressValue))
            {
                if (double.TryParse(options.ProgressValue, CultureInfo.InvariantCulture, out var parsed))
                {
                    if (parsed < 0.0 || parsed > 1.0)
                    {
                        onWarning?.Invoke(
                            $"Warning: Progress value {parsed} is out of range (0.0-1.0). " +
                            "Clamping to valid range.");
                        parsed = Math.Clamp(parsed, 0.0, 1.0);
                    }
                    progressValue = parsed;
                }
                else
                {
                    onWarning?.Invoke(
                        $"Warning: Invalid progress value \"{options.ProgressValue}\". " +
                        "Expected a number between 0.0 and 1.0, or \"indeterminate\".");
                }
            }

            builder.AddProgressBar(
                title: Shortcodes.ExpandIf(options.ProgressTitle, expand),
                value: progressValue,
                isIndeterminate: isIndeterminate,
                valueStringOverride: Shortcodes.ExpandIf(options.ProgressValueString, expand),
                status: Shortcodes.ExpandIf(options.ProgressStatus, expand) ?? DefaultProgressStatus);
        }

        // ── Timestamp ───────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Timestamp))
        {
            if (DateTime.TryParse(options.Timestamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var ts))
            {
                builder.AddCustomTimeStamp(ts);
            }
            else
            {
                onWarning?.Invoke(
                    $"Warning: Invalid timestamp \"{options.Timestamp}\". " +
                    "Expected ISO 8601 format (e.g., \"2025-01-15T09:00:00\").");
            }
        }

        // ── Launch URI ──────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.LaunchUri))
        {
            if (Uri.TryCreate(options.LaunchUri, UriKind.Absolute, out var launchUri))
            {
                builder.SetProtocolActivation(launchUri);
            }
            else
            {
                onWarning?.Invoke(
                    $"Warning: Invalid launch URI \"{options.LaunchUri}\". " +
                    "Toast will not have a launch action.");
            }
        }

        return builder;
    }

    private static void ConfigureNotification(ToastNotification toast, ToastOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Tag))
            toast.Tag = options.Tag;

        if (!string.IsNullOrWhiteSpace(options.Group))
            toast.Group = options.Group;

        if (options.Expiration.HasValue)
        {
            if (options.Expiration.Value > 0)
                toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(options.Expiration.Value);
            // Values <= 0 are silently ignored (validated with warning in Program.cs)
        }
    }

    // ── URI Resolution ──────────────────────────────────────────────

    internal static Uri ResolveImageUri(string input, Action<string>? onWarning = null)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme == "https")
                return absolute;

            if (absolute.Scheme == "http")
            {
                onWarning?.Invoke(
                    $"Warning: Loading image over insecure HTTP: \"{input}\". " +
                    "Consider using HTTPS.");
                return absolute;
            }

            if (absolute.Scheme == "file")
                return absolute;
        }

        var fullPath = Path.GetFullPath(input);
        if (!File.Exists(fullPath))
            onWarning?.Invoke($"Warning: Image file not found: \"{fullPath}\".");

        return new Uri(fullPath);
    }

    internal static Uri ResolveAudioUri(string audio)
    {
        if (SoundMap.TryGetValue(audio, out var mapped))
            return new Uri(mapped);

        // Allow raw ms-winsoundevent URIs
        if (audio.StartsWith("ms-winsoundevent:", StringComparison.OrdinalIgnoreCase))
            return new Uri(audio);

        // Otherwise treat as file path
        return new Uri(Path.GetFullPath(audio));
    }

    private static Dictionary<string, string> BuildSoundMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"]  = "ms-winsoundevent:Notification.Default",
            ["im"]       = "ms-winsoundevent:Notification.IM",
            ["mail"]     = "ms-winsoundevent:Notification.Mail",
            ["reminder"] = "ms-winsoundevent:Notification.Reminder",
            ["sms"]      = "ms-winsoundevent:Notification.SMS",
        };

        for (var i = 1; i <= MaxSoundVariants; i++)
        {
            var suffix = i == 1 ? "" : i.ToString();
            map[$"alarm{suffix}"] = $"ms-winsoundevent:Notification.Looping.Alarm{suffix}";
            map[$"call{suffix}"] = $"ms-winsoundevent:Notification.Looping.Call{suffix}";
        }

        return map;
    }

    // ── Button Parsing ──────────────────────────────────────────────

    private static void AddButton(
        ToastContentBuilder builder, string buttonSpec, Action<string>? onWarning, bool expand)
    {
        // Structured format: label=...,action=...
        if (buttonSpec.StartsWith("label=", StringComparison.OrdinalIgnoreCase))
        {
            var props = ParseKeyValuePairs(buttonSpec);
            if (!props.TryGetValue("label", out var label) || string.IsNullOrWhiteSpace(label))
            {
                onWarning?.Invoke($"Warning: Ignoring button with empty label: \"{buttonSpec}\"");
                return;
            }

            label = Shortcodes.ExpandIf(label, expand)!;
            var action = props.GetValueOrDefault("action") ?? "dismiss";
            var arguments = props.GetValueOrDefault("arguments");
            AddButtonByAction(builder, label, action, arguments, onWarning, buttonSpec);
            return;
        }

        // Legacy format: "Label" or "Label;action"
        var parts = buttonSpec.Split(';', 2);
        var legacyLabel = parts[0].Trim();

        if (string.IsNullOrWhiteSpace(legacyLabel))
        {
            onWarning?.Invoke($"Warning: Ignoring button with empty label: \"{buttonSpec}\"");
            return;
        }

        legacyLabel = Shortcodes.ExpandIf(legacyLabel, expand)!;

        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
        {
            AddButtonByAction(builder, legacyLabel, parts[1].Trim(), null, onWarning, buttonSpec);
        }
        else
        {
            builder.AddButton(new ToastButtonDismiss(legacyLabel));
        }
    }

    private static void AddButtonByAction(
        ToastContentBuilder builder, string label, string action,
        string? customArguments, Action<string>? onWarning, string originalSpec)
    {
        switch (action.ToLowerInvariant())
        {
            case "dismiss":
                builder.AddButton(new ToastButtonDismiss(label));
                break;

            case "submit":
                // Foreground activation — triggers Activated event with UserInput
                builder.AddButton(new ToastButton()
                    .SetContent(label)
                    .AddArgument("button", customArguments ?? label));
                break;

            default:
                // Try as URI for protocol activation
                if (Uri.TryCreate(action, UriKind.Absolute, out var uri))
                {
                    builder.AddButton(new ToastButton()
                        .SetContent(label)
                        .SetProtocolActivation(uri));
                }
                else
                {
                    onWarning?.Invoke(
                        $"Warning: Invalid button action \"{action}\" for \"{label}\". " +
                        "Valid actions: submit, dismiss, or a URI. Using dismiss.");
                    builder.AddButton(new ToastButtonDismiss(label));
                }
                break;
        }
    }

    internal static Dictionary<string, string> ParseKeyValuePairs(string spec)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in spec.Split(','))
        {
            var eq = pair.IndexOf('=');
            if (eq > 0)
            {
                result[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
            }
        }
        return result;
    }

    // ── XML Build + Post-Processing ─────────────────────────────────

    /// <summary>
    /// Build the toast XML string, applying all options including styling,
    /// extra texts, columns, raw text elements, and XML fragments.
    /// </summary>
    internal static string BuildXml(ToastOptions options, Action<string>? onWarning)
    {
        var builder = BuildToast(options, onWarning);
        var xml = builder.GetToastContent().GetContent();

        if (!NeedsPostProcessing(options))
            return xml;

        return PostProcessXml(xml, options);
    }

    private static bool NeedsPostProcessing(ToastOptions o) =>
        o.TitleStyle is not null || o.TitleAlign is not null
        || o.MessageStyle is not null || o.MessageAlign is not null
        || o.BodyStyle is not null || o.BodyAlign is not null
        || o.ExtraTexts.Count > 0
        || o.Groups.Count > 0
        || o.RawTextElements.Count > 0
        || o.XmlFragments.Count > 0;

    private static string PostProcessXml(string xml, ToastOptions options)
    {
        var doc = XDocument.Parse(xml);
        var toast = doc.Root
            ?? throw new InvalidOperationException("Toast XML has no root element.");

        var visual = toast.Element("visual")
            ?? throw new InvalidOperationException("Toast XML has no <visual> element.");

        var binding = visual.Element("binding")
            ?? throw new InvalidOperationException("Toast XML has no <binding> element.");

        // Identify Title/Message/Body texts (top-level <text> without placement="attribution").
        var contentTexts = binding.Elements("text")
            .Where(t => t.Attribute("placement")?.Value != "attribution")
            .ToList();

        // Apply per-line styling to existing texts.
        var perLine = new[]
        {
            (Style: options.TitleStyle,   Align: options.TitleAlign,   Label: "title"),
            (Style: options.MessageStyle, Align: options.MessageAlign, Label: "message"),
            (Style: options.BodyStyle,    Align: options.BodyAlign,    Label: "body"),
        };
        for (var i = 0; i < contentTexts.Count && i < perLine.Length; i++)
        {
            var (style, align, label) = perLine[i];
            ApplyStyleAttributes(contentTexts[i], style, align, label);
        }

        // Pull the attribution text out (if any) so we can append in correct order
        // and re-insert it as the last <text> child afterward.
        var attribution = binding.Elements("text")
            .FirstOrDefault(t => t.Attribute("placement")?.Value == "attribution");
        attribution?.Remove();

        // Append ExtraTexts as <text> elements with optional styling.
        foreach (var extra in options.ExtraTexts)
        {
            var element = new XElement("text",
                Shortcodes.ExpandIf(extra.Text, options.ExpandShortcodes) ?? "");
            ApplyStyleAttributes(element, extra.Style, extra.Align, "extra-text");
            binding.Add(element);
        }

        // Append RawTextElements verbatim.
        foreach (var raw in options.RawTextElements)
        {
            var element = ParseRawTextElement(raw, options.ValidateXml);
            binding.Add(element);
        }

        // Append Groups (each row → <group> with one <subgroup> per column).
        foreach (var row in options.Groups)
        {
            if (row.Count == 0) continue;
            var group = new XElement("group");
            foreach (var cell in row)
            {
                var sub = new XElement("subgroup");
                var text = new XElement("text",
                    Shortcodes.ExpandIf(cell.Text, options.ExpandShortcodes) ?? "");
                ApplyStyleAttributes(text, cell.Style, cell.Align, "column");
                sub.Add(text);
                group.Add(sub);
            }
            binding.Add(group);
        }

        // Re-add attribution at the very end of binding.
        if (attribution is not null)
            binding.Add(attribution);

        // XML fragment injection.
        foreach (var fragment in options.XmlFragments)
        {
            var elements = ParseXmlFragment(fragment.Fragment, options.ValidateXml);
            var anchor = ResolveAnchor(toast, fragment.Anchor);
            foreach (var el in elements)
                anchor.Add(el);
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static void ApplyStyleAttributes(
        XElement element, string? style, string? align, string fieldLabel)
    {
        var resolvedStyle = StyleResolver.ResolveStyle(style, fieldLabel);
        if (resolvedStyle is not null)
            element.SetAttributeValue("hint-style", resolvedStyle);

        var resolvedAlign = StyleResolver.ResolveAlign(align, fieldLabel);
        if (resolvedAlign is not null)
            element.SetAttributeValue("hint-align", resolvedAlign);
    }

    private static XElement ParseRawTextElement(string raw, bool validate)
    {
        XElement element;
        try
        {
            element = XElement.Parse(raw);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException(
                $"Invalid raw text element XML: {ex.Message}\nFragment: {raw}", ex);
        }

        if (validate && !element.Name.LocalName.Equals("text", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Raw text element must be a <text> element, got <{element.Name.LocalName}>. " +
                $"Use XmlFragments / --xml-fragment for arbitrary elements.");
        }

        return element;
    }

    private static IReadOnlyList<XElement> ParseXmlFragment(string fragment, bool validate)
    {
        // Wrap so multiple sibling elements parse as a single document.
        var wrapped = $"<root>{fragment}</root>";
        XElement root;
        try
        {
            root = XElement.Parse(wrapped);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException(
                $"Invalid XML fragment: {ex.Message}\nFragment: {fragment}", ex);
        }

        var elements = root.Elements().ToList();
        if (validate && elements.Count == 0)
        {
            throw new InvalidOperationException(
                $"XML fragment must contain at least one element. Fragment: {fragment}");
        }

        return elements;
    }

    private static XElement ResolveAnchor(XElement toast, XmlAnchor anchor)
    {
        switch (anchor)
        {
            case XmlAnchor.Toast:
                return toast;

            case XmlAnchor.Actions:
                var actions = toast.Element("actions");
                if (actions is null)
                {
                    actions = new XElement("actions");
                    toast.Add(actions);
                }
                return actions;

            case XmlAnchor.Binding:
            default:
                var visual = toast.Element("visual")
                    ?? throw new InvalidOperationException(
                        "Cannot inject into <binding>: <visual> element missing.");
                return visual.Element("binding")
                    ?? throw new InvalidOperationException(
                        "Cannot inject into <binding>: element missing.");
        }
    }

    private static ToastNotification CreateNotification(string xml)
    {
        var doc = new Windows.Data.Xml.Dom.XmlDocument();
        doc.LoadXml(xml);
        return new ToastNotification(doc);
    }

    /// <summary>
    /// Resolve the right Show callable for these options. If a personality
    /// (or one-off DisplayName/AppIcon) is set, ensures it is registered
    /// with Windows and returns a notifier bound to its AUMID. Otherwise
    /// returns the default Compat notifier (existing behavior).
    /// </summary>
    private static Action<ToastNotification> GetNotifier(
        ToastOptions options, Action<string>? onWarning)
    {
        var aumid = ResolveAndEnsurePersonality(options, onWarning);
        if (aumid is null)
        {
            var compat = ToastNotificationManagerCompat.CreateToastNotifier();
            return toast => compat.Show(toast);
        }

        var notifier = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(aumid);
        return toast => notifier.Show(toast);
    }

    /// <summary>
    /// Determine the AUMID to use for these options, registering on demand.
    /// Returns null if no personality is in play (use default notifier).
    /// </summary>
    private static string? ResolveAndEnsurePersonality(
        ToastOptions options, Action<string>? onWarning)
    {
        var config = PresetStore.LoadConfig();

        // Effective personality: explicit --as wins, else config defaultPersonality.
        var personalityName = !string.IsNullOrWhiteSpace(options.Personality)
            ? options.Personality
            : config.DefaultPersonality;

        // One-off DisplayName/AppIcon overrides build an ad-hoc personality.
        if (!string.IsNullOrWhiteSpace(options.DisplayName)
            || !string.IsNullOrWhiteSpace(options.AppIcon))
        {
            // Use the explicit personality name as a stable handle when given,
            // else derive one from the display name.
            var name = personalityName
                ?? options.DisplayName
                ?? "adhoc";
            var personality = new Personality
            {
                DisplayName = options.DisplayName ?? name,
                Icon = options.AppIcon
                    ?? (personalityName is not null
                        ? PersonalityStore.GetPersonality(personalityName)?.Icon
                        : null),
            };
            if (string.IsNullOrWhiteSpace(personality.Icon))
            {
                onWarning?.Invoke(
                    $"Warning: --display-name set without --app-icon and no resolvable " +
                    $"icon for personality \"{name}\". Falling back to default identity.");
                return null;
            }
            try
            {
                var entry = PersonalityStore.Register(name, personality, config, onWarning);
                return entry.Aumid;
            }
            catch (Exception ex)
            {
                onWarning?.Invoke(
                    $"Warning: Failed to register ad-hoc personality \"{name}\": {ex.Message}. " +
                    "Falling back to default identity.");
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(personalityName))
            return null;

        var configured = PersonalityStore.GetPersonality(personalityName);
        if (configured is null)
        {
            onWarning?.Invoke(
                $"Warning: Personality \"{personalityName}\" not found in config. " +
                "Use 'palantir personality list' to see available personalities. " +
                "Falling back to default identity.");
            return null;
        }

        // Lazy register: if shortcut missing or registry entry stale, (re)register.
        try
        {
            var aumid = PersonalityStore.ComputeAumid(personalityName, config);
            var infos = PersonalityStore.List(config);
            var info = infos.FirstOrDefault(
                i => i.Name.Equals(personalityName, StringComparison.OrdinalIgnoreCase));
            if (info is null || !info.RegisteredInWindows)
            {
                PersonalityStore.Register(personalityName, configured, config, onWarning);
            }
            return aumid;
        }
        catch (Exception ex)
        {
            onWarning?.Invoke(
                $"Warning: Failed to register personality \"{personalityName}\": {ex.Message}. " +
                "Falling back to default identity.");
            return null;
        }
    }
}

/// <summary>
/// Represents a toast notification entry from the history.
/// </summary>
public sealed class HistoryEntry
{
    public string? Tag { get; set; }
    public string? Group { get; set; }
    public List<string> Texts { get; set; } = [];
    public DateTimeOffset? ExpirationTime { get; set; }
}
