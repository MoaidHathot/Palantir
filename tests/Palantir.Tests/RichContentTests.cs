using System.Xml.Linq;
using Xunit;
using Palantir;

namespace Palantir.Tests;

/// <summary>
/// Tests for the rich-text/styling/layout/escape-hatch features added in v1.3.
/// </summary>
public class RichContentTests
{
    // ── Snapshot guard: zero behavior change for plain options ──────

    [Fact]
    public void GetXml_PlainOptions_UnchangedByPostProcessing()
    {
        // A toast with no new fields set should produce identical XML
        // to what the underlying builder emits — no post-processing applied.
        var options = new ToastOptions
        {
            Title = "Hello",
            Message = "World",
            Body = "Body",
            Attribution = "Via Palantir",
        };

        var xml = ToastService.GetXml(options);
        var doc = XDocument.Parse(xml);
        var binding = doc.Descendants("binding").Single();
        var texts = binding.Elements("text").ToList();

        // 4 text elements; none should carry hint-style/hint-align attributes.
        Assert.Equal(4, texts.Count);
        Assert.All(texts, t =>
        {
            Assert.Null(t.Attribute("hint-style"));
            Assert.Null(t.Attribute("hint-align"));
        });
    }

    // ── Per-line styling ────────────────────────────────────────────

    [Fact]
    public void GetXml_TitleStyleFriendlyAlias_MapsToSchemaValue()
    {
        var options = new ToastOptions
        {
            Title = "Hello",
            TitleStyle = "large",
            TitleAlign = "center",
        };

        var xml = ToastService.GetXml(options);
        var firstText = XDocument.Parse(xml).Descendants("text").First();

        Assert.Equal("title", firstText.Attribute("hint-style")?.Value);
        Assert.Equal("center", firstText.Attribute("hint-align")?.Value);
    }

    [Fact]
    public void GetXml_AllLineStyles_AppliedIndependently()
    {
        var options = new ToastOptions
        {
            Title = "T", Message = "M", Body = "B",
            TitleStyle = "header",
            MessageStyle = "dim",
            BodyStyle = "small",
            TitleAlign = "left",
            MessageAlign = "right",
        };

        var xml = ToastService.GetXml(options);
        var texts = XDocument.Parse(xml).Descendants("text").ToList();

        Assert.Equal("header", texts[0].Attribute("hint-style")?.Value);
        Assert.Equal("left",   texts[0].Attribute("hint-align")?.Value);
        Assert.Equal("baseSubtle", texts[1].Attribute("hint-style")?.Value);
        Assert.Equal("right",  texts[1].Attribute("hint-align")?.Value);
        Assert.Equal("caption", texts[2].Attribute("hint-style")?.Value);
    }

    [Fact]
    public void GetXml_RawSchemaStyleValue_PassedThrough()
    {
        var options = new ToastOptions
        {
            Title = "X",
            TitleStyle = "titleNumeral",
        };

        var xml = ToastService.GetXml(options);
        var firstText = XDocument.Parse(xml).Descendants("text").First();

        Assert.Equal("titleNumeral", firstText.Attribute("hint-style")?.Value);
    }

    [Fact]
    public void GetXml_InvalidStyle_ThrowsWithHelpfulMessage()
    {
        var options = new ToastOptions
        {
            Title = "X",
            TitleStyle = "huge",
        };

        var ex = Assert.Throws<ArgumentException>(() => ToastService.GetXml(options));
        Assert.Contains("Invalid style", ex.Message);
        Assert.Contains("Friendly aliases", ex.Message);
    }

    [Fact]
    public void GetXml_InvalidAlign_ThrowsWithHelpfulMessage()
    {
        var options = new ToastOptions
        {
            Title = "X",
            TitleAlign = "middle",
        };

        var ex = Assert.Throws<ArgumentException>(() => ToastService.GetXml(options));
        Assert.Contains("Invalid alignment", ex.Message);
    }

    // ── Extra text lines ────────────────────────────────────────────

    [Fact]
    public void GetXml_ExtraTexts_AppendedAfterContent()
    {
        var options = new ToastOptions
        {
            Title = "T", Message = "M",
            ExtraTexts =
            {
                new TextLine { Text = "Extra 1", Style = "dim" },
                new TextLine { Text = "Extra 2", Align = "right" },
            },
        };

        var xml = ToastService.GetXml(options);
        var texts = XDocument.Parse(xml).Descendants("text")
            .Where(t => t.Attribute("placement")?.Value != "attribution").ToList();

        Assert.Equal(4, texts.Count);
        Assert.Equal("Extra 1", texts[2].Value);
        Assert.Equal("baseSubtle", texts[2].Attribute("hint-style")?.Value);
        Assert.Equal("Extra 2", texts[3].Value);
        Assert.Equal("right", texts[3].Attribute("hint-align")?.Value);
    }

    [Fact]
    public void GetXml_ExtraTexts_AttributionStaysLast()
    {
        var options = new ToastOptions
        {
            Title = "T",
            Attribution = "Via X",
            ExtraTexts = { new TextLine { Text = "Extra" } },
        };

        var xml = ToastService.GetXml(options);
        var allTexts = XDocument.Parse(xml).Descendants("text").ToList();

        Assert.Equal("attribution", allTexts.Last().Attribute("placement")?.Value);
        Assert.Equal("Via X", allTexts.Last().Value);
    }

    // ── Group/subgroup layout ───────────────────────────────────────

    [Fact]
    public void GetXml_SingleRowColumns_EmitsGroupWithSubgroups()
    {
        var options = new ToastOptions
        {
            Title = "Backup",
            Groups =
            {
                new List<TextLine>
                {
                    new() { Text = "Started:", Style = "dim" },
                    new() { Text = "10:42 AM", Align = "right" },
                },
            },
        };

        var xml = ToastService.GetXml(options);
        var groups = XDocument.Parse(xml).Descendants("group").ToList();

        Assert.Single(groups);
        var subgroups = groups[0].Elements("subgroup").ToList();
        Assert.Equal(2, subgroups.Count);
        Assert.Equal("Started:", subgroups[0].Element("text")!.Value);
        Assert.Equal("baseSubtle", subgroups[0].Element("text")!.Attribute("hint-style")?.Value);
        Assert.Equal("10:42 AM", subgroups[1].Element("text")!.Value);
        Assert.Equal("right", subgroups[1].Element("text")!.Attribute("hint-align")?.Value);
    }

    [Fact]
    public void GetXml_MultiRowColumns_EmitsMultipleGroups()
    {
        var options = new ToastOptions
        {
            Title = "Backup",
            Groups =
            {
                new() { new() { Text = "A1" }, new() { Text = "A2" } },
                new() { new() { Text = "B1" }, new() { Text = "B2" } },
            },
        };

        var xml = ToastService.GetXml(options);
        var groups = XDocument.Parse(xml).Descendants("group").ToList();

        Assert.Equal(2, groups.Count);
        Assert.Equal("A1", groups[0].Descendants("text").First().Value);
        Assert.Equal("B2", groups[1].Descendants("text").Last().Value);
    }

    // ── Raw text elements ───────────────────────────────────────────

    [Fact]
    public void GetXml_RawTextElement_AppendedVerbatim()
    {
        var options = new ToastOptions
        {
            Title = "X",
            RawTextElements = { "<text hint-style=\"titleNumeral\" hint-align=\"center\">42</text>" },
        };

        var xml = ToastService.GetXml(options);
        var allTexts = XDocument.Parse(xml).Descendants("text").ToList();

        Assert.Contains(allTexts, t =>
            t.Value == "42" &&
            t.Attribute("hint-style")?.Value == "titleNumeral" &&
            t.Attribute("hint-align")?.Value == "center");
    }

    [Fact]
    public void GetXml_RawTextElement_NonTextRoot_ThrowsWhenValidating()
    {
        var options = new ToastOptions
        {
            Title = "X",
            RawTextElements = { "<group/>" },
            ValidateXml = true,
        };

        var ex = Assert.Throws<InvalidOperationException>(() => ToastService.GetXml(options));
        Assert.Contains("must be a <text> element", ex.Message);
    }

    [Fact]
    public void GetXml_RawTextElement_MalformedXml_ThrowsWhenValidating()
    {
        var options = new ToastOptions
        {
            Title = "X",
            RawTextElements = { "<text>unterminated" },
            ValidateXml = true,
        };

        Assert.Throws<InvalidOperationException>(() => ToastService.GetXml(options));
    }

    // ── XML fragments ───────────────────────────────────────────────

    [Fact]
    public void GetXml_XmlFragment_InjectedAtBindingByDefault()
    {
        var options = new ToastOptions
        {
            Title = "X",
            XmlFragments =
            {
                new XmlFragment { Fragment = "<image src=\"file:///c:/x.png\" placement=\"hero\"/>" },
            },
        };

        var xml = ToastService.GetXml(options);
        var binding = XDocument.Parse(xml).Descendants("binding").Single();

        Assert.Contains(binding.Elements("image"), i =>
            i.Attribute("placement")?.Value == "hero");
    }

    [Fact]
    public void GetXml_XmlFragment_InjectedIntoActions()
    {
        var options = new ToastOptions
        {
            Title = "X",
            XmlFragments =
            {
                new XmlFragment
                {
                    Fragment = "<action content=\"Custom\" arguments=\"a\" activationType=\"foreground\"/>",
                    Anchor = XmlAnchor.Actions,
                },
            },
        };

        var xml = ToastService.GetXml(options);
        var actions = XDocument.Parse(xml).Descendants("actions").FirstOrDefault();

        Assert.NotNull(actions);
        Assert.Contains(actions!.Elements("action"), a =>
            a.Attribute("content")?.Value == "Custom");
    }

    [Fact]
    public void GetXml_XmlFragment_MalformedXml_Throws()
    {
        var options = new ToastOptions
        {
            Title = "X",
            XmlFragments = { new XmlFragment { Fragment = "<bad" } },
        };

        Assert.Throws<InvalidOperationException>(() => ToastService.GetXml(options));
    }

    // ── Shortcode expansion ─────────────────────────────────────────

    [Fact]
    public void GetXml_Shortcodes_OffByDefault_LeavesLiteralText()
    {
        var options = new ToastOptions { Title = ":check: Done" };
        var xml = ToastService.GetXml(options);

        Assert.Contains(":check:", xml);
    }

    [Fact]
    public void GetXml_Shortcodes_WhenEnabled_ExpandsKnownCodes()
    {
        var options = new ToastOptions
        {
            Title = ":check: Done",
            Message = "Status: :warn: warning",
            ExpandShortcodes = true,
        };

        var xml = ToastService.GetXml(options);
        Assert.Contains("\u2705", xml);          // ✅
        Assert.Contains("\u26A0\uFE0F", xml);    // ⚠️
        Assert.DoesNotContain(":check:", xml);
        Assert.DoesNotContain(":warn:", xml);
    }

    [Fact]
    public void GetXml_Shortcodes_UnknownLeftAsLiteral()
    {
        var options = new ToastOptions
        {
            Title = ":nonsense_code: Hi",
            ExpandShortcodes = true,
        };

        var xml = ToastService.GetXml(options);
        Assert.Contains(":nonsense_code:", xml);
    }

    [Fact]
    public void GetXml_Shortcodes_ExpandsInExtraTextsAndColumns()
    {
        var options = new ToastOptions
        {
            Title = "X",
            ExpandShortcodes = true,
            ExtraTexts = { new TextLine { Text = ":star: extra" } },
            Groups = { new() { new() { Text = ":fire: hot" } } },
        };

        var xml = ToastService.GetXml(options);
        Assert.Contains("\u2B50", xml);          // ⭐
        Assert.Contains("\uD83D\uDD25", xml);    // 🔥
    }

    // ── StyleResolver direct tests ──────────────────────────────────

    [Theory]
    [InlineData("header", "header")]
    [InlineData("large",  "title")]
    [InlineData("normal", "base")]
    [InlineData("small",  "caption")]
    [InlineData("dim",    "baseSubtle")]
    [InlineData("LARGE",  "title")]
    public void StyleResolver_FriendlyAlias_Resolves(string input, string expected)
    {
        Assert.Equal(expected, StyleResolver.ResolveStyle(input, "test"));
    }

    [Theory]
    [InlineData("titleNumeral")]
    [InlineData("baseSubtle")]
    [InlineData("subheaderNumeral")]
    public void StyleResolver_RawSchemaValue_PassesThrough(string input)
    {
        Assert.Equal(input, StyleResolver.ResolveStyle(input, "test"));
    }

    // ── Shortcodes direct tests ─────────────────────────────────────

    [Fact]
    public void Shortcodes_Expand_HandlesMultipleInOneString()
    {
        var result = Shortcodes.Expand(":check: built, :rocket: deployed");
        Assert.Equal("\u2705 built, \uD83D\uDE80 deployed", result);
    }

    [Fact]
    public void Shortcodes_ExpandIf_FalseLeavesUnchanged()
    {
        Assert.Equal(":check:", Shortcodes.ExpandIf(":check:", enabled: false));
    }
}
