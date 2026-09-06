using System;
using RAMView.Core.Models;

namespace RAMView.Core.Algorithms;

/// <summary>
/// Calculated 2D boundary rectangle for a visual process node within the treemap.
/// </summary>
public record TreemapRectangle
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required ProcessMemoryInfo Process { get; init; }
    public bool IsHighlighted { get; set; } = true;

    public double AspectRatio
    {
        get
        {
            if (Width <= 0 || Height <= 0) return double.PositiveInfinity;
            return Math.Max(Width / Height, Height / Width);
        }
    }
}
