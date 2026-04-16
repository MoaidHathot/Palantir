using Microsoft.Toolkit.Uwp.Notifications;

namespace Palantir;

/// <summary>
/// Builds and shows Windows Toast Notifications from <see cref="ToastOptions"/>.
/// </summary>
public static class ToastService
{
    /// <summary>
    /// Build and display a toast notification from the given options.
    /// </summary>
    public static void Show(ToastOptions options)
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
            var uri = ResolveUri(options.Image);
            var crop = options.CropCircle
                ? ToastGenericAppLogoCrop.Circle
                : ToastGenericAppLogoCrop.Default;
            builder.AddAppLogoOverride(uri, crop);
        }

        if (!string.IsNullOrWhiteSpace(options.HeroImage))
        {
            var uri = ResolveUri(options.HeroImage);
            builder.AddHeroImage(uri);
        }

        if (!string.IsNullOrWhiteSpace(options.InlineImage))
        {
            var uri = ResolveUri(options.InlineImage);
            builder.AddInlineImage(uri);
        }

        // ── Buttons ─────────────────────────────────────────────────
        foreach (var buttonSpec in options.Buttons)
        {
            var parts = buttonSpec.Split(';', 2);
            var label = parts[0].Trim();

            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                var action = parts[1].Trim();
                builder.AddButton(new ToastButton()
                    .SetContent(label)
                    .SetProtocolActivation(new Uri(action)));
            }
            else
            {
                builder.AddButton(new ToastButtonDismiss(label));
            }
        }

        // ── Text Inputs ─────────────────────────────────────────────
        foreach (var inputSpec in options.Inputs)
        {
            var parts = inputSpec.Split(';', 2);
            var id = parts[0].Trim();
            var placeholder = parts.Length == 2 ? parts[1].Trim() : null;
            builder.AddInputTextBox(id, placeholder);
        }

        // ── Selection Inputs ────────────────────────────────────────
        foreach (var selectionSpec in options.Selections)
        {
            var parts = selectionSpec.Split(';', 2);
            if (parts.Length < 2) continue;

            var id = parts[0].Trim();
            var choices = parts[1].Split(',');

            var selectionBox = new ToastSelectionBox(id);
            foreach (var choice in choices)
            {
                var trimmed = choice.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    selectionBox.Items.Add(new ToastSelectionBoxItem(trimmed, trimmed));
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
            builder.SetToastDuration(options.Duration.ToLowerInvariant() switch
            {
                "long" => ToastDuration.Long,
                _ => ToastDuration.Short,
            });
        }

        // ── Scenario ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Scenario))
        {
            builder.SetToastScenario(options.Scenario.ToLowerInvariant() switch
            {
                "alarm" => ToastScenario.Alarm,
                "reminder" => ToastScenario.Reminder,
                "incomingcall" => ToastScenario.IncomingCall,
                _ => ToastScenario.Default,
            });
        }

        // ── Progress Bar ────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.ProgressValue) ||
            !string.IsNullOrWhiteSpace(options.ProgressTitle) ||
            !string.IsNullOrWhiteSpace(options.ProgressStatus))
        {
            var isIndeterminate = string.Equals(options.ProgressValue, "indeterminate", StringComparison.OrdinalIgnoreCase);
            double? progressValue = null;

            if (!isIndeterminate && !string.IsNullOrWhiteSpace(options.ProgressValue))
            {
                if (double.TryParse(options.ProgressValue, out var parsed))
                    progressValue = parsed;
            }

            builder.AddProgressBar(
                title: options.ProgressTitle,
                value: progressValue,
                isIndeterminate: isIndeterminate,
                valueStringOverride: options.ProgressValueString,
                status: options.ProgressStatus ?? "In progress");
        }

        // ── Timestamp ───────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.Timestamp))
        {
            if (DateTime.TryParse(options.Timestamp, out var ts))
                builder.AddCustomTimeStamp(ts);
        }

        // ── Launch URI ──────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(options.LaunchUri))
        {
            builder.SetProtocolActivation(new Uri(options.LaunchUri));
        }

        // ── Build and Show ──────────────────────────────────────────
        builder.Show(toast =>
        {
            if (!string.IsNullOrWhiteSpace(options.Tag))
                toast.Tag = options.Tag;

            if (!string.IsNullOrWhiteSpace(options.Group))
                toast.Group = options.Group;

            if (options.Expiration.HasValue && options.Expiration.Value > 0)
                toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(options.Expiration.Value);
        });
    }

    /// <summary>
    /// Clears all toast notification history for this app.
    /// </summary>
    public static void ClearHistory()
    {
        ToastNotificationManagerCompat.History.Clear();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static Uri ResolveUri(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == "http" || absolute.Scheme == "https"))
        {
            return absolute;
        }

        var fullPath = Path.GetFullPath(input);
        return new Uri(fullPath);
    }

    private static Uri ResolveAudioUri(string audio)
    {
        var soundMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"]  = "ms-winsoundevent:Notification.Default",
            ["im"]       = "ms-winsoundevent:Notification.IM",
            ["mail"]     = "ms-winsoundevent:Notification.Mail",
            ["reminder"] = "ms-winsoundevent:Notification.Reminder",
            ["sms"]      = "ms-winsoundevent:Notification.SMS",
        };

        // Support alarm, alarm2-10 and call, call2-10
        for (var i = 1; i <= 10; i++)
        {
            var suffix = i == 1 ? "" : i.ToString();
            var alarmKey = $"alarm{suffix}";
            var callKey = $"call{suffix}";
            soundMap[alarmKey] = $"ms-winsoundevent:Notification.Looping.Alarm{(i == 1 ? "" : i.ToString())}";
            soundMap[callKey] = $"ms-winsoundevent:Notification.Looping.Call{(i == 1 ? "" : i.ToString())}";
        }

        if (soundMap.TryGetValue(audio, out var mapped))
            return new Uri(mapped);

        // If it starts with ms-winsoundevent:, use as-is
        if (audio.StartsWith("ms-winsoundevent:", StringComparison.OrdinalIgnoreCase))
            return new Uri(audio);

        // Otherwise treat as file path
        return new Uri(Path.GetFullPath(audio));
    }
}
