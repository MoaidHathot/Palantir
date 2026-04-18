using System.Globalization;
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
        var builder = BuildToast(options, onWarning);

        builder.Show(toast =>
        {
            ConfigureNotification(toast, options);
        });
    }

    /// <summary>
    /// Show a toast and wait for user interaction. Returns the result as JSON-serializable object.
    /// </summary>
    public static WaitResult ShowAndWait(
        ToastOptions options, int? timeoutSeconds = null, Action<string>? onWarning = null)
    {
        var builder = BuildToast(options, onWarning);
        var waitHandle = new ManualResetEventSlim(false);
        WaitResult? result = null;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            result ??= new WaitResult { Action = "cancelled" };
            waitHandle.Set();
        };

        builder.Show(toast =>
        {
            ConfigureNotification(toast, options);

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
        });

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
    {
        var builder = BuildToast(options, onWarning);
        return builder.GetToastContent().GetContent();
    }

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

        // ── Text Content ────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Title))
            builder.AddText(options.Title);

        if (!string.IsNullOrWhiteSpace(options.Message))
            builder.AddText(options.Message);

        if (!string.IsNullOrWhiteSpace(options.Body))
            builder.AddText(options.Body);

        if (!string.IsNullOrWhiteSpace(options.Attribution))
            builder.AddAttributionText(options.Attribution);

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
            AddButton(builder, buttonSpec, onWarning);
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

            var placeholder = parts.Length == 2 ? parts[1].Trim() : null;
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
                var trimmed = choice.Trim();
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
            builder.AddHeader(
                options.HeaderId,
                options.HeaderTitle ?? options.HeaderId,
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
                title: options.ProgressTitle,
                value: progressValue,
                isIndeterminate: isIndeterminate,
                valueStringOverride: options.ProgressValueString,
                status: options.ProgressStatus ?? DefaultProgressStatus);
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
        ToastContentBuilder builder, string buttonSpec, Action<string>? onWarning)
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
