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

    /// <summary>The action that occurred: "activated", "dismissed", "failed", or "cancelled".</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    /// <summary>Activation arguments (e.g. button protocol URI).</summary>
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
}
