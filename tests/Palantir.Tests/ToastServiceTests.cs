using Xunit;
using Palantir;

namespace Palantir.Tests;

public class ToastServiceTests
{
    // ── ResolveAudioUri Tests ───────────────────────────────────────

    [Theory]
    [InlineData("default", "ms-winsoundevent:Notification.Default")]
    [InlineData("im", "ms-winsoundevent:Notification.IM")]
    [InlineData("mail", "ms-winsoundevent:Notification.Mail")]
    [InlineData("reminder", "ms-winsoundevent:Notification.Reminder")]
    [InlineData("sms", "ms-winsoundevent:Notification.SMS")]
    [InlineData("DEFAULT", "ms-winsoundevent:Notification.Default")]
    [InlineData("Mail", "ms-winsoundevent:Notification.Mail")]
    public void ResolveAudioUri_NamedSounds_ReturnsCorrectUri(string input, string expected)
    {
        var uri = ToastService.ResolveAudioUri(input);
        Assert.Equal(expected, uri.OriginalString);
    }

    [Theory]
    [InlineData("alarm", "ms-winsoundevent:Notification.Looping.Alarm")]
    [InlineData("alarm2", "ms-winsoundevent:Notification.Looping.Alarm2")]
    [InlineData("alarm10", "ms-winsoundevent:Notification.Looping.Alarm10")]
    [InlineData("call", "ms-winsoundevent:Notification.Looping.Call")]
    [InlineData("call5", "ms-winsoundevent:Notification.Looping.Call5")]
    [InlineData("ALARM", "ms-winsoundevent:Notification.Looping.Alarm")]
    [InlineData("Call3", "ms-winsoundevent:Notification.Looping.Call3")]
    public void ResolveAudioUri_AlarmAndCallSounds_ReturnsCorrectUri(string input, string expected)
    {
        var uri = ToastService.ResolveAudioUri(input);
        Assert.Equal(expected, uri.OriginalString);
    }

    [Fact]
    public void ResolveAudioUri_RawWinSoundEvent_PassesThrough()
    {
        var input = "ms-winsoundevent:Notification.Looping.Custom";
        var uri = ToastService.ResolveAudioUri(input);
        Assert.Equal(input, uri.OriginalString);
    }

    [Fact]
    public void ResolveAudioUri_FilePath_ReturnsFileUri()
    {
        var uri = ToastService.ResolveAudioUri("sound.wav");
        Assert.Equal("file", uri.Scheme);
        Assert.EndsWith("sound.wav", uri.LocalPath);
    }

    // ── ResolveImageUri Tests ───────────────────────────────────────

    [Fact]
    public void ResolveImageUri_HttpsUrl_ReturnsSameUri()
    {
        var uri = ToastService.ResolveImageUri("https://example.com/image.png");
        Assert.Equal("https", uri.Scheme);
        Assert.Equal("https://example.com/image.png", uri.OriginalString);
    }

    [Fact]
    public void ResolveImageUri_HttpUrl_ReturnsUriAndWarns()
    {
        var warnings = new List<string>();
        var uri = ToastService.ResolveImageUri("http://example.com/image.png", w => warnings.Add(w));

        Assert.Equal("http", uri.Scheme);
        Assert.Single(warnings);
        Assert.Contains("insecure HTTP", warnings[0]);
    }

    [Fact]
    public void ResolveImageUri_NonExistentFile_WarnsButReturnsUri()
    {
        var warnings = new List<string>();
        var uri = ToastService.ResolveImageUri("nonexistent_image_12345.png", w => warnings.Add(w));

        Assert.Equal("file", uri.Scheme);
        Assert.Single(warnings);
        Assert.Contains("not found", warnings[0]);
    }

    [Fact]
    public void ResolveImageUri_NoWarningCallback_DoesNotThrow()
    {
        // Should not throw even without a warning callback
        var uri = ToastService.ResolveImageUri("http://example.com/img.png");
        Assert.Equal("http", uri.Scheme);
    }

    // ── Validation Tests ────────────────────────────────────────────

    [Theory]
    [InlineData("short")]
    [InlineData("long")]
    public void ValidDurations_ContainsExpectedValues(string duration)
    {
        Assert.Contains(duration, ToastService.ValidDurations);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("alarm")]
    [InlineData("reminder")]
    [InlineData("incomingcall")]
    public void ValidScenarios_ContainsExpectedValues(string scenario)
    {
        Assert.Contains(scenario, ToastService.ValidScenarios);
    }

    // ── GetXml Tests (dry-run) ──────────────────────────────────────

    [Fact]
    public void GetXml_BasicToast_ReturnsValidXml()
    {
        var options = new ToastOptions { Title = "Test", Message = "Hello" };
        var xml = ToastService.GetXml(options);

        Assert.Contains("Test", xml);
        Assert.Contains("Hello", xml);
        Assert.Contains("<toast", xml);
    }

    [Fact]
    public void GetXml_WithAttribution_IncludesAttributionInXml()
    {
        var options = new ToastOptions
        {
            Title = "Build",
            Message = "Done",
            Attribution = "Via CI",
        };
        var xml = ToastService.GetXml(options);

        Assert.Contains("Via CI", xml);
    }

    [Fact]
    public void GetXml_WithProgressBar_IncludesProgressInXml()
    {
        var options = new ToastOptions
        {
            Title = "Download",
            ProgressValue = "0.5",
            ProgressStatus = "Downloading...",
        };
        var xml = ToastService.GetXml(options);

        Assert.Contains("progress", xml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetXml_InvalidProgressValue_WarnsAndClamps()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            ProgressValue = "5.0",
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("out of range", warnings[0]);
    }

    [Fact]
    public void GetXml_InvalidDuration_WarnsButProceeds()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Duration = "medium",
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("Invalid duration", warnings[0]);
        // Invalid duration defaults to Short; the XML still generates successfully
        Assert.Contains("<toast", xml);
    }

    [Fact]
    public void GetXml_InvalidScenario_WarnsButProceeds()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Scenario = "bogus",
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("Invalid scenario", warnings[0]);
    }

    [Fact]
    public void GetXml_InvalidTimestamp_Warns()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Timestamp = "not-a-date",
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("Invalid timestamp", warnings[0]);
    }

    [Fact]
    public void GetXml_InvalidLaunchUri_Warns()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            LaunchUri = "not a valid uri",
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("Invalid launch URI", warnings[0]);
    }

    [Fact]
    public void GetXml_InvalidButtonUri_WarnsAndFallsToDismiss()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Buttons = ["Click;not-a-uri"],
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("Invalid button action", warnings[0]);
    }

    [Fact]
    public void GetXml_EmptyButtonLabel_WarnsAndSkips()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Buttons = [";https://example.com"],
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("empty label", warnings[0]);
    }

    [Fact]
    public void GetXml_MalformedSelection_WarnsAndSkips()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Selections = ["missing-semicolon"],
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("malformed selection", warnings[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetXml_EmptyInputId_WarnsAndSkips()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Inputs = [";placeholder"],
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("empty id", warnings[0]);
    }

    [Fact]
    public void GetXml_WithHeader_IncludesHeaderInXml()
    {
        var options = new ToastOptions
        {
            Title = "Test",
            HeaderId = "campaign-1",
            HeaderTitle = "Campaign Updates",
        };
        var xml = ToastService.GetXml(options);

        Assert.Contains("campaign-1", xml);
        Assert.Contains("Campaign Updates", xml);
    }

    // ── WaitResult Tests ────────────────────────────────────────────

    [Fact]
    public void WaitResult_ToJson_OmitsNullFields()
    {
        var result = new WaitResult { Action = "activated" };
        var json = result.ToJson();

        Assert.Contains("\"action\":\"activated\"", json);
        Assert.DoesNotContain("arguments", json);
        Assert.DoesNotContain("reason", json);
        Assert.DoesNotContain("error", json);
        Assert.DoesNotContain("userInputs", json);
    }

    [Fact]
    public void WaitResult_ToJson_IncludesNonNullFields()
    {
        var result = new WaitResult
        {
            Action = "dismissed",
            Reason = "UserCanceled",
        };
        var json = result.ToJson();

        Assert.Contains("\"action\":\"dismissed\"", json);
        Assert.Contains("\"reason\":\"UserCanceled\"", json);
    }

    [Fact]
    public void WaitResult_ToJson_IncludesUserInputs()
    {
        var result = new WaitResult
        {
            Action = "activated",
            UserInputs = new Dictionary<string, string> { ["reply"] = "hello" },
        };
        var json = result.ToJson();

        Assert.Contains("\"userInputs\"", json);
        Assert.Contains("\"reply\":\"hello\"", json);
    }

    // ── DefaultProgressStatus Constant ──────────────────────────────

    [Fact]
    public void DefaultProgressStatus_HasExpectedValue()
    {
        Assert.Equal("In progress", ToastService.DefaultProgressStatus);
    }

    // ── Button Parsing (submit action) ──────────────────────────────

    [Fact]
    public void GetXml_SubmitButton_IncludesArguments()
    {
        var options = new ToastOptions
        {
            Title = "Reply",
            Buttons = ["Send;submit"],
        };

        var xml = ToastService.GetXml(options);

        Assert.Contains("Send", xml);
        // Submit buttons include arguments (used for foreground activation)
        Assert.Contains("arguments", xml);
    }

    [Fact]
    public void GetXml_DismissButton_ExplicitKeyword()
    {
        var options = new ToastOptions
        {
            Title = "Test",
            Buttons = ["Cancel;dismiss"],
        };

        var xml = ToastService.GetXml(options);

        Assert.Contains("Cancel", xml);
    }

    [Fact]
    public void GetXml_StructuredButton_ParsesCorrectly()
    {
        var options = new ToastOptions
        {
            Title = "Test",
            Buttons = ["label=Send,action=submit"],
        };

        var xml = ToastService.GetXml(options);

        Assert.Contains("Send", xml);
        Assert.Contains("arguments", xml);
    }

    [Fact]
    public void GetXml_StructuredButton_CustomArguments()
    {
        var options = new ToastOptions
        {
            Title = "Test",
            Buttons = ["label=Reply,action=submit,arguments=send-reply"],
        };

        var xml = ToastService.GetXml(options);

        Assert.Contains("Reply", xml);
        Assert.Contains("send-reply", xml);
    }

    [Fact]
    public void GetXml_InvalidAction_WarnsAndDismisses()
    {
        var warnings = new List<string>();
        var options = new ToastOptions
        {
            Title = "Test",
            Buttons = ["Click;not-valid"],
        };

        var xml = ToastService.GetXml(options, w => warnings.Add(w));

        Assert.Single(warnings);
        Assert.Contains("Invalid button action", warnings[0]);
    }

    // ── ParseKeyValuePairs ──────────────────────────────────────────

    [Fact]
    public void ParseKeyValuePairs_BasicParsing()
    {
        var result = ToastService.ParseKeyValuePairs("label=Send,action=submit");

        Assert.Equal("Send", result["label"]);
        Assert.Equal("submit", result["action"]);
    }

    [Fact]
    public void ParseKeyValuePairs_WithArguments()
    {
        var result = ToastService.ParseKeyValuePairs("label=OK,action=submit,arguments=confirm");

        Assert.Equal("OK", result["label"]);
        Assert.Equal("submit", result["action"]);
        Assert.Equal("confirm", result["arguments"]);
    }

    [Fact]
    public void ParseKeyValuePairs_CaseInsensitiveKeys()
    {
        var result = ToastService.ParseKeyValuePairs("Label=Test,Action=dismiss");

        Assert.True(result.ContainsKey("label"));
        Assert.True(result.ContainsKey("action"));
    }

    // ── WaitResult.ToText ───────────────────────────────────────────

    [Fact]
    public void WaitResult_ToText_BasicFormat()
    {
        var result = new WaitResult { Action = "activated" };
        var text = result.ToText();

        Assert.Equal("action=activated", text);
    }

    [Fact]
    public void WaitResult_ToText_WithAllFields()
    {
        var result = new WaitResult
        {
            Action = "activated",
            Arguments = "Send",
            UserInputs = new Dictionary<string, string> { ["reply"] = "hello" },
        };
        var text = result.ToText();

        Assert.Contains("action=activated", text);
        Assert.Contains("arguments=Send", text);
        Assert.Contains("input.reply=hello", text);
    }

    [Fact]
    public void WaitResult_ToText_Dismissed()
    {
        var result = new WaitResult
        {
            Action = "dismissed",
            Reason = "UserCanceled",
        };
        var text = result.ToText();

        Assert.Contains("action=dismissed", text);
        Assert.Contains("reason=UserCanceled", text);
    }
}
