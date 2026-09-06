using System;

namespace RAMView.Core.Models;

/// <summary>
/// Immutable snapshot record representing a running process's memory footprint and metadata.
/// Uses Working Set (physical RAM occupied) as the primary metric per specification §6.
/// </summary>
public record ProcessMemoryInfo
{
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public required long WorkingSet64 { get; init; }
    public double PercentageOfTracked { get; init; }
    public string? ExecutablePath { get; init; }
    public byte[]? IconPngBytes { get; init; }
    public bool IsResponding { get; init; } = true;
    public DateTime? StartTime { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int InstanceCount { get; init; } = 1;

    /// <summary>
    /// User-friendly memory formatting (e.g. "2.45 GB", "512.3 MB").
    /// </summary>
    public string FormattedMemory => FormatBytes(WorkingSet64);

    /// <summary>
    /// Formatted percentage string (e.g. "15.4%").
    /// </summary>
    public string FormattedPercentage => $"{PercentageOfTracked:F1}%";

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 MB";
        double kb = bytes / 1024.0;
        double mb = kb / 1024.0;
        double gb = mb / 1024.0;

        if (gb >= 1.0)
            return $"{gb:F2} GB";
        if (mb >= 1.0)
            return $"{mb:F1} MB";
        return $"{kb:F0} KB";
    }
}
