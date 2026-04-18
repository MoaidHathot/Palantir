using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Palantir;

/// <summary>
/// Represents the result of waiting for a toast notification interaction.
/// </summary>
public sealed class WaitResult
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The action that occurred: "activated", "dismissed", "failed", "cancelled", or "timedOut".</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    /// <summary>Activation arguments (e.g. button label for submit buttons).</summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Arguments { get; set; }

    /// <summary>Dismissal reason (e.g. "UserCanceled", "TimedOut", "ApplicationHidden").</summary>
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    /// <summary>Error message if the toast failed.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    /// <summary>User input values keyed by input ID.</summary>
    [JsonPropertyName("userInputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? UserInputs { get; set; }

    /// <summary>Serialize this result to a JSON string.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    /// <summary>Serialize this result to key=value text lines.</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"action={Action}");
        if (Arguments is not null) sb.AppendLine($"arguments={Arguments}");
        if (Reason is not null) sb.AppendLine($"reason={Reason}");
        if (Error is not null) sb.AppendLine($"error={Error}");
        if (UserInputs is not null)
        {
            foreach (var (key, value) in UserInputs)
                sb.AppendLine($"input.{key}={value}");
        }
        return sb.ToString().TrimEnd();
    }
}
