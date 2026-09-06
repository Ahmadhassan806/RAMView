using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Resilient, high-performance process monitoring service.
/// Queries running processes, aggregates Working Set memory, and diffs lifecycle changes.
/// </summary>
public class ProcessMonitor : IProcessProvider
{
    private readonly IIconExtractor _iconExtractor;

    public ProcessMonitor(IIconExtractor iconExtractor)
    {
        _iconExtractor = iconExtractor ?? throw new ArgumentNullException(nameof(iconExtractor));
    }

    public ValueTask<IReadOnlyList<ProcessMemoryInfo>> GetProcessesAsync(
        bool groupProcesses,
        bool showSystemProcesses,
        string filterPreset = "All",
        CancellationToken cancellationToken = default)
    {
        var rawProcesses = Process.GetProcesses();
        var collected = new List<ProcessMemoryInfo>(rawProcesses.Length);

        foreach (var p in rawProcesses)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                if (p.HasExited) continue;

                long workingSet = p.WorkingSet64;
                if (workingSet <= 0) continue;

                int pid = p.Id;
                string processName = p.ProcessName;

                bool isSystem = IsSystemProcess(pid, processName);
                if (!showSystemProcesses && isSystem) continue;

                string? exePath = null;
                DateTime? startTime = null;
                bool isResponding = true;

                try
                {
                    isResponding = p.Responding;
                }
                catch { }

                try
                {
                    exePath = p.MainModule?.FileName;
                }
                catch
                {
                    // Many system/protected processes deny MainModule access without admin
                }

                try
                {
                    startTime = p.StartTime;
                }
                catch { }

                collected.Add(new ProcessMemoryInfo
                {
                    ProcessId = pid,
                    ProcessName = processName,
                    WorkingSet64 = workingSet,
                    ExecutablePath = exePath,
                    IconPngBytes = null, // Populated after grouping & filtering for top items
                    IsResponding = isResponding,
                    StartTime = startTime,
                    InstanceCount = 1
                });
            }
            catch
            {
                // Silent isolation per process
            }
            finally
            {
                p.Dispose();
            }
        }

        // Apply Process Grouping (§11)
        List<ProcessMemoryInfo> resultList;
        if (groupProcesses)
        {
            resultList = collected
                .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var primary = g.OrderByDescending(x => x.WorkingSet64).First();
                    long totalWorkingSet = g.Sum(x => x.WorkingSet64);
                    int count = g.Count();

                    return new ProcessMemoryInfo
                    {
                        ProcessId = primary.ProcessId,
                        ProcessName = primary.ProcessName,
                        WorkingSet64 = totalWorkingSet,
                        ExecutablePath = primary.ExecutablePath,
                        IconPngBytes = null,
                        IsResponding = g.All(x => x.IsResponding),
                        StartTime = primary.StartTime,
                        InstanceCount = count
                    };
                })
                .ToList();
        }
        else
        {
            resultList = collected;
        }

        // Apply Filter Presets (§13)
        resultList = ApplyFilter(resultList, filterPreset);

        // Sort descending by WorkingSet64
        resultList = resultList.OrderByDescending(x => x.WorkingSet64).ToList();

        // Compute totalTrackedMemory (§6) and assign relative percentages & icons for visible items
        long totalTracked = resultList.Sum(x => x.WorkingSet64);
        int topIconCount = Math.Min(resultList.Count, 50);

        for (int i = 0; i < resultList.Count; i++)
        {
            var item = resultList[i];
            double percentage = totalTracked > 0 ? ((double)item.WorkingSet64 / totalTracked) * 100.0 : 0.0;
            byte[]? iconBytes = null;

            // Only extract icons for visible candidates (top 50) to keep polling under 15ms
            if (i < topIconCount)
            {
                iconBytes = _iconExtractor.GetIconPng(item.ExecutablePath, item.ProcessName);
            }

            resultList[i] = item with
            {
                PercentageOfTracked = percentage,
                IconPngBytes = iconBytes
            };
        }

        return ValueTask.FromResult<IReadOnlyList<ProcessMemoryInfo>>(resultList);
    }

    private static List<ProcessMemoryInfo> ApplyFilter(List<ProcessMemoryInfo> list, string filter)
    {
        return filter switch
        {
            "Highest" => list.OrderByDescending(x => x.WorkingSet64).Take(25).ToList(),
            "System" => list.Where(x => IsSystemProcess(x.ProcessId, x.ProcessName)).ToList(),
            "Applications" => list.Where(x => !IsSystemProcess(x.ProcessId, x.ProcessName)).ToList(),
            _ => list
        };
    }

    private static bool IsSystemProcess(int pid, string name)
    {
        if (pid == 0 || pid == 4) return true;

        string[] systemNames =
        {
            "System", "Idle", "Registry", "smss", "csrss", "wininit", "services",
            "lsass", "svchost", "fontdrvhost", "winlogon", "dwm", "sihost", "Memory Compression"
        };

        return systemNames.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
