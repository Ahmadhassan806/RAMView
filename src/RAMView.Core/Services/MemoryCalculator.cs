using System;
using System.Collections.Generic;
using System.Linq;
using RAMView.Core.Models;

namespace RAMView.Core.Services;

/// <summary>
/// Handles memory calculations, floor clamping, anti-dominance adjustment,
/// and percentage computations per specification §6.
/// </summary>
public static class MemoryCalculator
{
    /// <summary>
    /// Computes adjusted layout weights for treemap rendering.
    /// Enforces minimum visual floor and anti-dominance capping (e.g. max 60% area).
    /// </summary>
    public static List<(ProcessMemoryInfo Process, double Weight)> CalculateLayoutWeights(
        IReadOnlyList<ProcessMemoryInfo> processes,
        long minFloorBytes = 20 * 1024 * 1024,
        double maxShareRatio = 0.60)
    {
        if (processes == null || processes.Count == 0)
            return new List<(ProcessMemoryInfo, double)>();

        // 1. Calculate raw weights applying the minimum floor
        var rawItems = new List<(ProcessMemoryInfo Process, double RawWeight)>(processes.Count);
        foreach (var p in processes)
        {
            // Ensure minimum visual floor so small processes do not disappear (§6)
            double weight = Math.Max(p.WorkingSet64, minFloorBytes);
            rawItems.Add((p, weight));
        }

        double totalRawWeight = rawItems.Sum(x => x.RawWeight);
        if (totalRawWeight <= 0)
        {
            return rawItems.Select(x => (x.Process, 1.0)).ToList();
        }

        // 2. Enforce Anti-Dominance Rule (§6):
        // No single process may occupy more than maxShareRatio (e.g. 60%) of the treemap.
        // If the top process exceeds this, cap it and proportionally scale the remainder.
        var adjusted = new List<(ProcessMemoryInfo Process, double Weight)>(rawItems.Count);
        double maxAllowedWeight = totalRawWeight * maxShareRatio;

        // Check if dominant items exist
        bool dominanceAdjusted = false;
        double nonDominantRawSum = 0;
        double dominantTotalAllocated = 0;
        var dominantIndices = new HashSet<int>();

        for (int i = 0; i < rawItems.Count; i++)
        {
            if (rawItems[i].RawWeight > maxAllowedWeight && rawItems.Count > 1)
            {
                dominantIndices.Add(i);
                dominantTotalAllocated += maxAllowedWeight;
                dominanceAdjusted = true;
            }
            else
            {
                nonDominantRawSum += rawItems[i].RawWeight;
            }
        }

        if (dominanceAdjusted && nonDominantRawSum > 0)
        {
            double remainingBudget = totalRawWeight - dominantTotalAllocated;
            if (remainingBudget <= 0)
            {
                remainingBudget = totalRawWeight * (1.0 - maxShareRatio);
            }

            for (int i = 0; i < rawItems.Count; i++)
            {
                if (dominantIndices.Contains(i))
                {
                    adjusted.Add((rawItems[i].Process, maxAllowedWeight));
                }
                else
                {
                    double scaledWeight = (rawItems[i].RawWeight / nonDominantRawSum) * remainingBudget;
                    adjusted.Add((rawItems[i].Process, Math.Max(scaledWeight, 1.0)));
                }
            }
        }
        else
        {
            foreach (var item in rawItems)
            {
                adjusted.Add((item.Process, item.RawWeight));
            }
        }

        // Stable ordering: sort descending by Weight, tie-break by ProcessName then ProcessId (§7)
        return adjusted
            .OrderByDescending(x => x.Weight)
            .ThenBy(x => x.Process.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Process.ProcessId)
            .ToList();
    }
}
