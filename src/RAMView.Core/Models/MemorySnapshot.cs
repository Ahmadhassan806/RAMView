using System;
using System.Collections.Generic;

namespace RAMView.Core.Models;

/// <summary>
/// Immutable snapshot representing overall system physical memory and all currently tracked processes.
/// </summary>
public record MemorySnapshot
{
    public required ulong TotalPhysicalBytes { get; init; }
    public required ulong UsedPhysicalBytes { get; init; }
    public required ulong AvailablePhysicalBytes { get; init; }
    public required long TotalTrackedBytes { get; init; }
    public required IReadOnlyList<ProcessMemoryInfo> Processes { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public double SystemMemoryUsagePercentage =>
        TotalPhysicalBytes > 0 ? ((double)UsedPhysicalBytes / TotalPhysicalBytes) * 100.0 : 0.0;

    public string FormattedTotal => ProcessMemoryInfo.FormatBytes((long)TotalPhysicalBytes);
    public string FormattedUsed => ProcessMemoryInfo.FormatBytes((long)UsedPhysicalBytes);
    public string FormattedAvailable => ProcessMemoryInfo.FormatBytes((long)AvailablePhysicalBytes);
    public string FormattedTracked => ProcessMemoryInfo.FormatBytes(TotalTrackedBytes);

    public string SummaryText => $"{FormattedUsed} / {FormattedTotal}";
}
