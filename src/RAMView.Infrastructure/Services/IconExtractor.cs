using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using RAMView.Core.Interfaces;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Extracts and caches application icons with dual code paths (§24):
/// 1) Win32 Icon extraction via ExtractAssociatedIcon / Shell.
/// 2) Package manifest / UWP resolution fallback for packaged store apps.
/// Results are cached in memory so each executable is extracted at most once.
/// Fully thread-safe and fast.
/// </summary>
public class IconExtractor : IIconExtractor
{
    private readonly ConcurrentDictionary<string, byte[]?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public byte[]? GetIconPng(string? executablePath, string processName)
    {
        string cacheKey = !string.IsNullOrEmpty(executablePath) ? executablePath : processName;
        if (_cache.TryGetValue(cacheKey, out var cachedBytes))
        {
            return cachedBytes;
        }

        byte[]? extracted = ExtractIcon(executablePath, processName);
        _cache[cacheKey] = extracted;
        return extracted;
    }

    private byte[]? ExtractIcon(string? path, string processName)
    {
        try
        {
            // Path 1: Classic Win32 EXE extraction via System.Drawing.Icon
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon != null)
                {
                    using var bmp = icon.ToBitmap();
                    using var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }

            // Path 2: Packaged / Modern app fallback resolution
            if (!string.IsNullOrEmpty(path) && path.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
            {
                byte[]? manifestIcon = TryExtractFromAppx(path);
                if (manifestIcon != null) return manifestIcon;
            }
        }
        catch
        {
            // Graceful non-throwing degradation (§22)
        }

        return null;
    }

    private static byte[]? TryExtractFromAppx(string exePath)
    {
        try
        {
            string? dir = Path.GetDirectoryName(exePath);
            if (dir == null) return null;

            string assetsDir = Path.Combine(dir, "Assets");
            if (Directory.Exists(assetsDir))
            {
                var files = Directory.GetFiles(assetsDir, "*targetsize-32*.png");
                if (files.Length > 0)
                {
                    return File.ReadAllBytes(files[0]);
                }

                files = Directory.GetFiles(assetsDir, "*Square44x44Logo*.png");
                if (files.Length > 0)
                {
                    return File.ReadAllBytes(files[0]);
                }
            }
        }
        catch
        {
            // Ignore restricted access
        }

        return null;
    }
}
