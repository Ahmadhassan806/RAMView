using System.Collections.Generic;
using System.Linq;
using RAMView.Core.Models;
using RAMView.Core.Services;
using Xunit;

namespace RAMView.Tests;

public class MemoryCalculatorTests
{
    [Fact]
    public void CalculateLayoutWeights_AppliesMinMemoryFloor()
    {
        long minFloorBytes = 20 * 1024 * 1024; // 20 MB

        var processes = new List<ProcessMemoryInfo>
        {
            new() { ProcessId = 1, ProcessName = "TinyApp", WorkingSet64 = 2 * 1024 * 1024 }, // 2 MB
            new() { ProcessId = 2, ProcessName = "BigApp", WorkingSet64 = 200 * 1024 * 1024 } // 200 MB
        };

        var weights = MemoryCalculator.CalculateLayoutWeights(processes, minFloorBytes, 0.90);

        var tiny = weights.First(x => x.Process.ProcessName == "TinyApp");
        // Weight should be bumped up to at least the minimum floor
        Assert.True(tiny.Weight >= minFloorBytes, "Tiny app weight must meet or exceed minimum floor");
    }

    [Fact]
    public void CalculateLayoutWeights_AppliesAntiDominanceRule()
    {
        double maxShareRatio = 0.60; // 60% max share (§6)

        var processes = new List<ProcessMemoryInfo>
        {
            new() { ProcessId = 1, ProcessName = "DominantChrome", WorkingSet64 = 9000L * 1024 * 1024 }, // 9 GB
            new() { ProcessId = 2, ProcessName = "SmallApp", WorkingSet64 = 500L * 1024 * 1024 },        // 500 MB
            new() { ProcessId = 3, ProcessName = "OtherApp", WorkingSet64 = 500L * 1024 * 1024 }         // 500 MB
        };

        var weights = MemoryCalculator.CalculateLayoutWeights(processes, minFloorBytes: 20 * 1024 * 1024, maxShareRatio: maxShareRatio);

        double totalWeight = weights.Sum(x => x.Weight);
        var dominant = weights.First(x => x.Process.ProcessName == "DominantChrome");

        double dominantShare = dominant.Weight / totalWeight;

        // Dominant process should be capped at or below maxShareRatio (approx 60%)
        Assert.True(dominantShare <= maxShareRatio + 0.01,
            $"Dominant process share ({dominantShare:P1}) must not exceed {maxShareRatio:P1}");
    }

    [Fact]
    public void CalculateLayoutWeights_MaintainsStableSorting()
    {
        var processes = new List<ProcessMemoryInfo>
        {
            new() { ProcessId = 2, ProcessName = "Beta", WorkingSet64 = 100 * 1024 * 1024 },
            new() { ProcessId = 1, ProcessName = "Alpha", WorkingSet64 = 100 * 1024 * 1024 },
            new() { ProcessId = 3, ProcessName = "Gamma", WorkingSet64 = 500 * 1024 * 1024 }
        };

        var weights = MemoryCalculator.CalculateLayoutWeights(processes);

        Assert.Equal("Gamma", weights[0].Process.ProcessName);
        // Ties in weight sorted by name alphabetically
        Assert.Equal("Alpha", weights[1].Process.ProcessName);
        Assert.Equal("Beta", weights[2].Process.ProcessName);
    }
}
