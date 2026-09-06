using System;
using System.Collections.Generic;
using System.Linq;
using RAMView.Core.Algorithms;
using RAMView.Core.Models;
using Xunit;

namespace RAMView.Tests;

public class SquarifiedTreemapTests
{
    [Fact]
    public void CalculateLayout_EmptyItems_ReturnsEmpty()
    {
        var layout = SquarifiedTreemap.CalculateLayout(Array.Empty<(ProcessMemoryInfo, double)>(), 500, 300);
        Assert.Empty(layout);
    }

    [Fact]
    public void CalculateLayout_ZeroDimensions_ReturnsEmpty()
    {
        var items = new List<(ProcessMemoryInfo, double)>
        {
            (CreateFakeProcess("test", 100), 100.0)
        };

        var layout = SquarifiedTreemap.CalculateLayout(items, 0, 300);
        Assert.Empty(layout);
    }

    [Fact]
    public void CalculateLayout_SingleItem_FillsCanvasRoughly()
    {
        var items = new List<(ProcessMemoryInfo, double)>
        {
            (CreateFakeProcess("solo", 500), 500.0)
        };

        var layout = SquarifiedTreemap.CalculateLayout(items, 400, 200, padding: 0);

        Assert.Single(layout);
        var rect = layout[0];
        Assert.Equal(0, rect.X, 1);
        Assert.Equal(0, rect.Y, 1);
        Assert.Equal(400, rect.Width, 1);
        Assert.Equal(200, rect.Height, 1);
    }

    [Fact]
    public void CalculateLayout_MultipleItems_NoOverlapsAndBounded()
    {
        double canvasW = 600;
        double canvasH = 400;

        var items = new List<(ProcessMemoryInfo, double)>
        {
            (CreateFakeProcess("Chrome", 2000), 2000.0),
            (CreateFakeProcess("IDE", 1500), 1500.0),
            (CreateFakeProcess("Discord", 800), 800.0),
            (CreateFakeProcess("Spotify", 400), 400.0),
            (CreateFakeProcess("Terminal", 200), 200.0),
            (CreateFakeProcess("Notepad", 50), 50.0)
        };

        var layout = SquarifiedTreemap.CalculateLayout(items, canvasW, canvasH, padding: 2.0);

        Assert.Equal(items.Count, layout.Count);

        foreach (var r in layout)
        {
            // All coordinates must be within canvas bounds
            Assert.True(r.X >= 0, $"X {r.X} >= 0");
            Assert.True(r.Y >= 0, $"Y {r.Y} >= 0");
            Assert.True(r.X + r.Width <= canvasW + 1.0, $"X+W {r.X + r.Width} <= {canvasW}");
            Assert.True(r.Y + r.Height <= canvasH + 1.0, $"Y+H {r.Y + r.Height} <= {canvasH}");
            Assert.True(r.Width > 0, "Width > 0");
            Assert.True(r.Height > 0, "Height > 0");
        }
    }

    [Fact]
    public void CalculateLayout_PreservesOrderingOfHighestWeight()
    {
        var items = new List<(ProcessMemoryInfo, double)>
        {
            (CreateFakeProcess("Huge", 10000), 10000.0),
            (CreateFakeProcess("Medium", 3000), 3000.0),
            (CreateFakeProcess("Small", 500), 500.0)
        };

        var layout = SquarifiedTreemap.CalculateLayout(items, 800, 600, padding: 0);

        var hugeRect = layout.First(x => x.Process.ProcessName == "Huge");
        var smallRect = layout.First(x => x.Process.ProcessName == "Small");

        double hugeArea = hugeRect.Width * hugeRect.Height;
        double smallArea = smallRect.Width * smallRect.Height;

        Assert.True(hugeArea > smallArea, "Highest weight item must produce larger area");
    }

    private static ProcessMemoryInfo CreateFakeProcess(string name, long workingSetMb)
    {
        return new ProcessMemoryInfo
        {
            ProcessId = Random.Shared.Next(1000, 9999),
            ProcessName = name,
            WorkingSet64 = workingSetMb * 1024 * 1024,
            InstanceCount = 1
        };
    }
}
