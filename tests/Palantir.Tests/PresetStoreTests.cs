using Xunit;
using Palantir;

namespace Palantir.Tests;

public class PresetStoreTests
{
    // ── Built-in Presets ─────────────────────────────────────────────

    [Theory]
    [InlineData("alarm")]
    [InlineData("reminder")]
    [InlineData("call")]
    public void GetPreset_BuiltIn_ReturnsPreset(string name)
    {
        var preset = PresetStore.GetPreset(name);
        Assert.NotNull(preset);
    }

    [Fact]
    public void GetPreset_Unknown_ReturnsNull()
    {
        var preset = PresetStore.GetPreset("nonexistent-preset-xyz");
        Assert.Null(preset);
    }

    [Fact]
    public void IsBuiltIn_KnownPresets_ReturnsTrue()
    {
        Assert.True(PresetStore.IsBuiltIn("alarm"));
        Assert.True(PresetStore.IsBuiltIn("reminder"));
        Assert.True(PresetStore.IsBuiltIn("call"));
    }

    [Fact]
    public void IsBuiltIn_CaseInsensitive()
    {
        Assert.True(PresetStore.IsBuiltIn("ALARM"));
        Assert.True(PresetStore.IsBuiltIn("Reminder"));
    }

    [Fact]
    public void IsBuiltIn_Unknown_ReturnsFalse()
    {
        Assert.False(PresetStore.IsBuiltIn("custom"));
    }

    [Fact]
    public void GetBuiltInPresets_ContainsThreePresets()
    {
        var presets = PresetStore.GetBuiltInPresets();
        Assert.Equal(3, presets.Count);
        Assert.True(presets.ContainsKey("alarm"));
        Assert.True(presets.ContainsKey("reminder"));
        Assert.True(presets.ContainsKey("call"));
    }

    // ── Built-in Preset Values ──────────────────────────────────────

    [Fact]
    public void BuiltIn_Alarm_HasExpectedDefaults()
    {
        var preset = PresetStore.GetPreset("alarm")!;
        Assert.Equal("alarm", preset.Scenario);
        Assert.Equal("alarm", preset.Audio);
        Assert.True(preset.Loop);
        Assert.Equal("long", preset.Duration);
    }

    [Fact]
    public void BuiltIn_Reminder_HasExpectedDefaults()
    {
        var preset = PresetStore.GetPreset("reminder")!;
        Assert.Equal("reminder", preset.Scenario);
        Assert.Equal("reminder", preset.Audio);
        Assert.False(preset.Loop);
        Assert.Equal("long", preset.Duration);
    }

    [Fact]
    public void BuiltIn_Call_HasExpectedDefaults()
    {
        var preset = PresetStore.GetPreset("call")!;
        Assert.Equal("incomingCall", preset.Scenario);
        Assert.Equal("call", preset.Audio);
        Assert.True(preset.Loop);
        Assert.Equal("long", preset.Duration);
    }

    // ── MergePreset ─────────────────────────────────────────────────

    [Fact]
    public void MergePreset_AppliesNonNullValues()
    {
        var target = new ToastOptions { Title = "Hello" };
        var preset = new ToastOptions
        {
            Scenario = "alarm",
            Audio = "alarm",
            Loop = true,
            Duration = "long",
        };

        PresetStore.MergePreset(target, preset);

        Assert.Equal("Hello", target.Title);
        Assert.Equal("alarm", target.Scenario);
        Assert.Equal("alarm", target.Audio);
        Assert.True(target.Loop);
        Assert.Equal("long", target.Duration);
    }

    [Fact]
    public void MergePreset_DoesNotOverrideExistingValues()
    {
        var target = new ToastOptions
        {
            Audio = "mail",
            Duration = "short",
        };
        var preset = new ToastOptions
        {
            Audio = "alarm",
            Duration = "long",
            Scenario = "alarm",
        };

        var explicit_ = new HashSet<string> { "audio", "duration" };
        PresetStore.MergePreset(target, preset, explicit_);

        Assert.Equal("mail", target.Audio); // Explicit — not overridden
        Assert.Equal("short", target.Duration); // Explicit — not overridden
        Assert.Equal("alarm", target.Scenario); // Not explicit — merged
    }

    [Fact]
    public void MergePreset_BoolFields_OnlyTurnOn()
    {
        var target = new ToastOptions();
        var preset = new ToastOptions { Loop = true, Silent = true };

        PresetStore.MergePreset(target, preset);

        Assert.True(target.Loop);
        Assert.True(target.Silent);
    }

    [Fact]
    public void MergePreset_ArrayFields_OnlyApplyWhenTargetEmpty()
    {
        var target = new ToastOptions { Buttons = ["Existing"] };
        var preset = new ToastOptions { Buttons = ["Preset Button"] };

        PresetStore.MergePreset(target, preset);

        // Target already has buttons, so preset's buttons are NOT applied
        Assert.Single(target.Buttons);
        Assert.Equal("Existing", target.Buttons[0]);
    }

    [Fact]
    public void MergePreset_ArrayFields_ApplyWhenTargetIsEmpty()
    {
        var target = new ToastOptions();
        var preset = new ToastOptions { Buttons = ["Preset Button"] };

        PresetStore.MergePreset(target, preset);

        Assert.Single(target.Buttons);
        Assert.Equal("Preset Button", target.Buttons[0]);
    }

    [Fact]
    public void MergePreset_AllStringFields_Applied()
    {
        var target = new ToastOptions();
        var preset = new ToastOptions
        {
            Title = "T",
            Message = "M",
            Body = "B",
            Attribution = "A",
            Image = "I",
            HeroImage = "H",
            InlineImage = "IL",
            LaunchUri = "https://x.com",
            HeaderId = "hdr",
            HeaderTitle = "Header",
        };

        PresetStore.MergePreset(target, preset);

        Assert.Equal("T", target.Title);
        Assert.Equal("M", target.Message);
        Assert.Equal("B", target.Body);
        Assert.Equal("A", target.Attribution);
        Assert.Equal("I", target.Image);
        Assert.Equal("H", target.HeroImage);
        Assert.Equal("IL", target.InlineImage);
        Assert.Equal("https://x.com", target.LaunchUri);
        Assert.Equal("hdr", target.HeaderId);
        Assert.Equal("Header", target.HeaderTitle);
    }

    // ── Serialization ───────────────────────────────────────────────

    [Fact]
    public void DeserializePreset_FromJson_ReturnsCorrectOptions()
    {
        var json = """{"scenario":"alarm","audio":"mail","loop":true,"duration":"long"}""";
        var preset = PresetStore.DeserializePreset(json);

        Assert.Equal("alarm", preset.Scenario);
        Assert.Equal("mail", preset.Audio);
        Assert.True(preset.Loop);
        Assert.Equal("long", preset.Duration);
    }

    [Fact]
    public void DeserializePreset_CaseInsensitive()
    {
        var json = """{"Scenario":"alarm","Audio":"mail"}""";
        var preset = PresetStore.DeserializePreset(json);

        Assert.Equal("alarm", preset.Scenario);
        Assert.Equal("mail", preset.Audio);
    }

    [Fact]
    public void SerializePresetJson_OmitsDefaults()
    {
        var preset = new ToastOptions
        {
            Scenario = "alarm",
            Audio = "alarm",
            Loop = true,
        };

        var json = PresetStore.SerializePresetJson(preset);

        Assert.Contains("scenario", json);
        Assert.Contains("audio", json);
        Assert.Contains("loop", json);
        // Default values should not appear
        Assert.DoesNotContain("title", json);
        Assert.DoesNotContain("message", json);
        Assert.DoesNotContain("buttons", json);
        Assert.DoesNotContain("silent", json);
    }

    [Fact]
    public void SerializePresetJson_IncludesButtons()
    {
        var preset = new ToastOptions
        {
            Title = "Test",
            Buttons = ["OK", "Cancel;https://example.com"],
        };

        var json = PresetStore.SerializePresetJson(preset);

        Assert.Contains("title", json);
        Assert.Contains("buttons", json);
        Assert.Contains("OK", json);
        Assert.Contains("Cancel", json);
    }

    // ── Config Path Resolution ──────────────────────────────────────

    [Fact]
    public void GetConfigDirectory_ReturnsNonEmptyPath()
    {
        var dir = PresetStore.GetConfigDirectory();
        Assert.NotNull(dir);
        Assert.NotEmpty(dir);
    }

    [Fact]
    public void GetConfigFilePath_EndsWithConfigJson()
    {
        var path = PresetStore.GetConfigFilePath();
        Assert.EndsWith("palantir.json", path);
    }

    // ── FormatSummary ───────────────────────────────────────────────

    [Fact]
    public void FormatSummary_EmptyPreset_ReturnsEmpty()
    {
        var preset = new ToastOptions();
        var summary = PresetStore.FormatSummary(preset);
        Assert.Equal("(empty)", summary);
    }

    [Fact]
    public void FormatSummary_WithValues_IncludesKeyFields()
    {
        var preset = new ToastOptions
        {
            Scenario = "alarm",
            Audio = "alarm",
            Loop = true,
            Duration = "long",
        };

        var summary = PresetStore.FormatSummary(preset);

        Assert.Contains("scenario=alarm", summary);
        Assert.Contains("audio=alarm", summary);
        Assert.Contains("loop", summary);
        Assert.Contains("duration=long", summary);
    }
}
