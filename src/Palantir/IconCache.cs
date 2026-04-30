using System.Security.Cryptography;
using System.Text;

namespace Palantir;

/// <summary>
/// Caches personality icons. Accepts file paths, HTTP(S) URLs, .ico/.png/.jpg.
/// Always returns a path to a .ico file suitable for use as a Start Menu shortcut icon.
/// Non-ICO inputs are wrapped into an ICO container and cached on disk.
/// </summary>
public static class IconCache
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Resolve <paramref name="source"/> (path or URL) to a local .ico path,
    /// downloading and converting if necessary.
    /// </summary>
    public static string ResolveToIco(string source, PalantirConfig? config = null)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Icon source is empty.", nameof(source));

        var iconsDir = PathsResolver.EnsureDirectory(PathsResolver.GetIconsDirectory(config));

        // 1. HTTP(S) URL → download to cache (deterministic file name).
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            var fileName = HashFileName(source) + ExtensionFromUrl(uri);
            var localPath = Path.Combine(iconsDir, fileName);
            if (!File.Exists(localPath))
            {
                using var stream = Http.GetStreamAsync(uri).GetAwaiter().GetResult();
                using var file = File.Create(localPath);
                stream.CopyTo(file);
            }
            return EnsureIco(localPath, iconsDir);
        }

        // 2. Local path
        var fullPath = Path.GetFullPath(source);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Icon file not found: \"{fullPath}\"");

        return EnsureIco(fullPath, iconsDir);
    }

    /// <summary>
    /// If <paramref name="path"/> is already .ico, return it as-is.
    /// Otherwise produce a derived .ico (PNG embedded) in <paramref name="iconsDir"/>.
    /// </summary>
    public static string EnsureIco(string path, string iconsDir)
    {
        if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            return path;

        var derived = Path.Combine(
            iconsDir,
            Path.GetFileNameWithoutExtension(path) + "-" + HashFileName(path)[..8] + ".ico");

        if (!File.Exists(derived))
        {
            var bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(derived, WrapPngInIco(bytes));
        }
        return derived;
    }

    /// <summary>
    /// Wrap raw image bytes (PNG works directly, JPEG accepted as PNG-compatible payload)
    /// into a Vista+ embedded-PNG ICO container.
    /// </summary>
    /// <remarks>
    /// ICO header (6 bytes) + ICONDIRENTRY (16 bytes) + image data. Windows Vista
    /// and later accept PNG payloads inside ICO; for JPEG we still wrap (Windows
    /// is forgiving for shortcut icons but result may render at low quality).
    /// </remarks>
    public static byte[] WrapPngInIco(byte[] image)
    {
        var (width, height) = TryReadPngDimensions(image) ?? (0, 0);

        // ICO requires 0 in the dimension byte for 256x256.
        byte wByte = (byte)(width >= 256 ? 0 : width);
        byte hByte = (byte)(height >= 256 ? 0 : height);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // ICONDIR: reserved, type=1 (icon), count=1
        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)1);

        // ICONDIRENTRY
        bw.Write(wByte);            // width
        bw.Write(hByte);            // height
        bw.Write((byte)0);          // color count (0 = >256)
        bw.Write((byte)0);          // reserved
        bw.Write((ushort)1);        // planes
        bw.Write((ushort)32);       // bits per pixel
        bw.Write(image.Length);     // size of image data
        bw.Write(22);               // offset (6 + 16)

        bw.Write(image);
        return ms.ToArray();
    }

    private static (int Width, int Height)? TryReadPngDimensions(byte[] data)
    {
        // PNG: 8-byte signature, then IHDR chunk: length(4)+"IHDR"(4)+width(4 BE)+height(4 BE)
        if (data.Length < 24) return null;
        var pngSig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (var i = 0; i < pngSig.Length; i++)
            if (data[i] != pngSig[i]) return null;

        // After 8-byte sig: 4-byte length, 4-byte "IHDR", then 4-byte width, 4-byte height (big-endian).
        var width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        var height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return (width, height);
    }

    private static string HashFileName(string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string ExtensionFromUrl(Uri uri)
    {
        var ext = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrEmpty(ext) ? ".png" : ext.ToLowerInvariant();
    }

    /// <summary>Clear the icon cache directory.</summary>
    public static int Clear(PalantirConfig? config = null)
    {
        var dir = PathsResolver.GetIconsDirectory(config);
        if (!Directory.Exists(dir)) return 0;
        var count = Directory.GetFiles(dir).Length;
        Directory.Delete(dir, recursive: true);
        return count;
    }
}
