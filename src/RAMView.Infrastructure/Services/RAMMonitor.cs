using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;
using RAMView.Infrastructure.Native;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Native memory monitor querying physical RAM via Win32 GlobalMemoryStatusEx.
/// </summary>
public class RAMMonitor : IMemoryMonitor
{
    public ValueTask<MemorySnapshot> GetSystemMemorySnapshotAsync(CancellationToken cancellationToken = default)
    {
        var memStatus = new NativeMethods.MEMORYSTATUSEX();
        if (!NativeMethods.GlobalMemoryStatusEx(memStatus))
        {
            // Fallback in case of unexpected failure
            var gcInfo = GC.GetGCMemoryInfo();
            ulong total = (ulong)gcInfo.TotalAvailableMemoryBytes;
            ulong used = (ulong)gcInfo.MemoryLoadBytes;
            ulong avail = total > used ? total - used : 0;

            return ValueTask.FromResult(new MemorySnapshot
            {
                TotalPhysicalBytes = total,
                UsedPhysicalBytes = used,
                AvailablePhysicalBytes = avail,
                TotalTrackedBytes = 0,
                Processes = Array.Empty<ProcessMemoryInfo>()
            });
        }

        ulong totalPhys = memStatus.ullTotalPhys;
        ulong availPhys = memStatus.ullAvailPhys;
        ulong usedPhys = totalPhys > availPhys ? totalPhys - availPhys : 0;

        return ValueTask.FromResult(new MemorySnapshot
        {
            TotalPhysicalBytes = totalPhys,
            UsedPhysicalBytes = usedPhys,
            AvailablePhysicalBytes = availPhys,
            TotalTrackedBytes = 0,
            Processes = Array.Empty<ProcessMemoryInfo>()
        });
    }
}
