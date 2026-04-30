using Xunit;
using Palantir;

namespace Palantir.Tests;

/// <summary>
/// Tests for path resolution, icon caching, and personality storage.
/// </summary>
public class PersonalityTests
{
    // ── PathsResolver ───────────────────────────────────────────────

    [Fact]
    public void PathsResolver_ExplicitConfig_Wins()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "palantir-test-" + Guid.NewGuid().ToString("N"));
        var config = new PalantirConfig
        {
            Paths = new PalantirPaths
            {
                Cache = tmp,
                Icons = Path.Combine(tmp, "ic"),
                Images = Path.Combine(tmp, "im"),
                Registry = Path.Combine(tmp, "reg.json"),
            },
        };

        Assert.Equal(tmp, PathsResolver.GetCacheDirectory(config));
        Assert.Equal(Path.Combine(tmp, "ic"), PathsResolver.GetIconsDirectory(config));
        Assert.Equal(Path.Combine(tmp, "im"), PathsResolver.GetImagesDirectory(config));
        Assert.Equal(Path.Combine(tmp, "reg.json"), PathsResolver.GetRegistryFilePath(config));
    }

    [Fact]
    public void PathsResolver_DerivesIconsAndImagesFromCache()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "palantir-test-" + Guid.NewGuid().ToString("N"));
        var config = new PalantirConfig { Paths = new PalantirPaths { Cache = tmp } };

        Assert.Equal(Path.Combine(tmp, "icons"), PathsResolver.GetIconsDirectory(config));
        Assert.Equal(Path.Combine(tmp, "images"), PathsResolver.GetImagesDirectory(config));
    }

    [Fact]
    public void PathsResolver_DefaultsExistAndAreAbsolute()
    {
        var config = new PalantirConfig();
        var cache = PathsResolver.GetCacheDirectory(config);
        Assert.True(Path.IsPathRooted(cache), $"cache path should be absolute: {cache}");

        var icons = PathsResolver.GetIconsDirectory(config);
        Assert.True(Path.IsPathRooted(icons));
    }

    [Fact]
    public void PathsResolver_EnsureDirectory_Creates()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "palantir-ed-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.False(Directory.Exists(tmp));
            PathsResolver.EnsureDirectory(tmp);
            Assert.True(Directory.Exists(tmp));
        }
        finally
        {
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        }
    }

    // ── IconCache: PNG → ICO wrapping ───────────────────────────────

    [Fact]
    public void IconCache_WrapPngInIco_ProducesValidHeader()
    {
        // Minimal valid PNG: 8-byte sig + IHDR length(13) + "IHDR" + 13 bytes data + CRC.
        // For our header-only inspection we just need width/height bytes correct.
        var png = MakeMinimalPng(width: 32, height: 48);
        var ico = IconCache.WrapPngInIco(png);

        // ICONDIR
        Assert.Equal(0, ico[0]);
        Assert.Equal(0, ico[1]);
        Assert.Equal(1, ico[2]);  // type=icon
        Assert.Equal(0, ico[3]);
        Assert.Equal(1, ico[4]);  // count=1
        Assert.Equal(0, ico[5]);

        // ICONDIRENTRY
        Assert.Equal(32, ico[6]);   // width
        Assert.Equal(48, ico[7]);   // height
        Assert.Equal(0, ico[8]);    // colour count
        Assert.Equal(22, ico[18]);  // offset

        // PNG payload starts at offset 22
        Assert.Equal(png.Length, ico.Length - 22);
        for (var i = 0; i < png.Length; i++)
            Assert.Equal(png[i], ico[22 + i]);
    }

    [Fact]
    public void IconCache_WrapPngInIco_HandlesLargeDimensions()
    {
        var png = MakeMinimalPng(width: 256, height: 256);
        var ico = IconCache.WrapPngInIco(png);
        // 256x256 must encode as 0 in width/height byte slots.
        Assert.Equal(0, ico[6]);
        Assert.Equal(0, ico[7]);
    }

    [Fact]
    public void IconCache_ResolveToIco_LocalIco_ReturnsAsIs()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "palantir-icotest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var icoPath = Path.Combine(tmp, "fake.ico");
        File.WriteAllBytes(icoPath, new byte[] { 0, 0, 1, 0, 1, 0 });

        var config = new PalantirConfig { Paths = new PalantirPaths { Icons = tmp } };
        try
        {
            var resolved = IconCache.ResolveToIco(icoPath, config);
            Assert.Equal(icoPath, resolved);
        }
        finally
        {
            Directory.Delete(tmp, true);
        }
    }

    [Fact]
    public void IconCache_ResolveToIco_PngLocal_ProducesIcoInCache()
    {
        var srcDir = Path.Combine(Path.GetTempPath(), "palantir-pngsrc-" + Guid.NewGuid().ToString("N"));
        var iconsDir = Path.Combine(Path.GetTempPath(), "palantir-pngico-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(srcDir);
        var pngPath = Path.Combine(srcDir, "logo.png");
        File.WriteAllBytes(pngPath, MakeMinimalPng(64, 64));

        var config = new PalantirConfig { Paths = new PalantirPaths { Icons = iconsDir } };
        try
        {
            var resolved = IconCache.ResolveToIco(pngPath, config);
            Assert.True(File.Exists(resolved));
            Assert.EndsWith(".ico", resolved, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(iconsDir, resolved, StringComparison.OrdinalIgnoreCase);

            // Idempotent: second call returns same path without recreating.
            var resolved2 = IconCache.ResolveToIco(pngPath, config);
            Assert.Equal(resolved, resolved2);
        }
        finally
        {
            if (Directory.Exists(srcDir)) Directory.Delete(srcDir, true);
            if (Directory.Exists(iconsDir)) Directory.Delete(iconsDir, true);
        }
    }

    [Fact]
    public void IconCache_ResolveToIco_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            IconCache.ResolveToIco("Z:\\nope_does_not_exist.ico"));
    }

    // ── PersonalityStore: AUMID derivation ──────────────────────────

    [Fact]
    public void PersonalityStore_ComputeAumid_DefaultPrefix()
    {
        var aumid = PersonalityStore.ComputeAumid("opencode", new PalantirConfig());
        Assert.Equal("Palantir.opencode", aumid);
    }

    [Fact]
    public void PersonalityStore_ComputeAumid_CustomPrefix()
    {
        var aumid = PersonalityStore.ComputeAumid(
            "opencode", new PalantirConfig { AumidPrefix = "MyOrg" });
        Assert.Equal("MyOrg.opencode", aumid);
    }

    [Fact]
    public void PersonalityStore_ComputeAumid_SanitizesInput()
    {
        var aumid = PersonalityStore.ComputeAumid("My Personality!", new PalantirConfig());
        // Spaces and "!" are stripped; letters, digits, dot, dash, underscore kept.
        Assert.Equal("Palantir.MyPersonality", aumid);
    }

    [Fact]
    public void PersonalityStore_GetShortcutPath_UnderStartMenu()
    {
        var path = PersonalityStore.GetShortcutPath("OpenCode");
        Assert.Contains("Microsoft", path);
        Assert.Contains("Start Menu", path);
        Assert.EndsWith("OpenCode.lnk", path);
    }

    // ── Helper ──────────────────────────────────────────────────────

    private static byte[] MakeMinimalPng(int width, int height)
    {
        // Construct a tiny "PNG" with a valid signature + IHDR chunk header
        // sufficient for IconCache.WrapPngInIco's dimension reader. The image
        // body doesn't need to be valid for our wrapper-only tests.
        var bytes = new byte[57];
        // Signature
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        // IHDR length (13) big-endian
        bytes[8] = 0; bytes[9] = 0; bytes[10] = 0; bytes[11] = 13;
        // "IHDR"
        bytes[12] = (byte)'I'; bytes[13] = (byte)'H'; bytes[14] = (byte)'D'; bytes[15] = (byte)'R';
        // width big-endian
        bytes[16] = (byte)((width >> 24) & 0xff);
        bytes[17] = (byte)((width >> 16) & 0xff);
        bytes[18] = (byte)((width >> 8) & 0xff);
        bytes[19] = (byte)(width & 0xff);
        // height big-endian
        bytes[20] = (byte)((height >> 24) & 0xff);
        bytes[21] = (byte)((height >> 16) & 0xff);
        bytes[22] = (byte)((height >> 8) & 0xff);
        bytes[23] = (byte)(height & 0xff);
        // remaining bytes left as 0 (fine for this stub)
        return bytes;
    }
}
