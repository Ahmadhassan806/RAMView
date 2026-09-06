using System;
using System.Collections.Generic;
using System.Linq;
using RAMView.Core.Models;

namespace RAMView.Core.Algorithms;

/// <summary>
/// Implements the Squarified Treemap algorithm (Bruls, Huizing, van Wijk).
/// Organizes weighted process nodes into clean, roughly-square rectangular blocks,
/// avoiding long, narrow slivers and naive row/grid artifacts.
/// </summary>
public static class SquarifiedTreemap
{
    private record LayoutItem(ProcessMemoryInfo Process, double Area);

    public static List<TreemapRectangle> CalculateLayout(
        IReadOnlyList<(ProcessMemoryInfo Process, double Weight)> weightedItems,
        double width,
        double height,
        double padding = 2.0)
    {
        var results = new List<TreemapRectangle>();

        if (weightedItems == null || weightedItems.Count == 0 || width <= 1 || height <= 1)
            return results;

        double totalWeight = weightedItems.Sum(x => x.Weight);
        if (totalWeight <= 0) return results;

        double totalArea = width * height;

        // Convert weights to geometric areas
        var items = new List<LayoutItem>(weightedItems.Count);
        foreach (var item in weightedItems)
        {
            double area = (item.Weight / totalWeight) * totalArea;
            if (area > 0)
            {
                items.Add(new LayoutItem(item.Process, area));
            }
        }

        if (items.Count == 0) return results;

        // Current available bounding rectangle
        double curX = 0;
        double curY = 0;
        double curW = width;
        double curH = height;

        var currentRow = new List<LayoutItem>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            double shortEdge = Math.Min(curW, curH);
            if (shortEdge <= 0.001) break;

            if (currentRow.Count == 0)
            {
                currentRow.Add(item);
                continue;
            }

            double currentWorst = WorstAspectRatio(currentRow, shortEdge);
            var testRow = new List<LayoutItem>(currentRow) { item };
            double testWorst = WorstAspectRatio(testRow, shortEdge);

            if (testWorst <= currentWorst)
            {
                // Adding item improves or maintains aspect ratio
                currentRow.Add(item);
            }
            else
            {
                // Adding item worsens aspect ratio: layout current row and start fresh row
                LayoutRow(currentRow, ref curX, ref curY, ref curW, ref curH, results, padding);
                currentRow.Clear();
                currentRow.Add(item);
            }
        }

        // Layout any remaining items in the final row
        if (currentRow.Count > 0)
        {
            LayoutRow(currentRow, ref curX, ref curY, ref curW, ref curH, results, padding);
        }

        return results;
    }

    private static double WorstAspectRatio(List<LayoutItem> row, double sideLength)
    {
        if (row.Count == 0 || sideLength <= 0) return double.PositiveInfinity;

        double sumArea = row.Sum(x => x.Area);
        if (sumArea <= 0) return double.PositiveInfinity;

        double maxArea = row.Max(x => x.Area);
        double minArea = row.Min(x => x.Area);

        double sideSq = sideLength * sideLength;
        double sumAreaSq = sumArea * sumArea;

        double term1 = (sideSq * maxArea) / sumAreaSq;
        double term2 = sumAreaSq / (sideSq * minArea);

        return Math.Max(term1, term2);
    }

    private static void LayoutRow(
        List<LayoutItem> row,
        ref double curX,
        ref double curY,
        ref double curW,
        ref double curH,
        List<TreemapRectangle> results,
        double padding)
    {
        if (row.Count == 0) return;

        double sumArea = row.Sum(x => x.Area);
        if (sumArea <= 0) return;

        bool isHorizontal = curW >= curH;
        double shortEdge = isHorizontal ? curH : curW;

        // Thickness of this row strip
        double stripThickness = sumArea / shortEdge;

        if (isHorizontal)
        {
            // Vertical strip dividing width
            double rowX = curX;
            double itemY = curY;

            foreach (var item in row)
            {
                double itemH = item.Area / stripThickness;
                double renderW = Math.Max(0, stripThickness - padding);
                double renderH = Math.Max(0, itemH - padding);

                results.Add(new TreemapRectangle
                {
                    X = rowX + (padding / 2.0),
                    Y = itemY + (padding / 2.0),
                    Width = renderW,
                    Height = renderH,
                    Process = item.Process
                });

                itemY += itemH;
            }

            curX += stripThickness;
            curW = Math.Max(0, curW - stripThickness);
        }
        else
        {
            // Horizontal strip dividing height
            double rowY = curY;
            double itemX = curX;

            foreach (var item in row)
            {
                double itemW = item.Area / stripThickness;
                double renderW = Math.Max(0, itemW - padding);
                double renderH = Math.Max(0, stripThickness - padding);

                results.Add(new TreemapRectangle
                {
                    X = itemX + (padding / 2.0),
                    Y = rowY + (padding / 2.0),
                    Width = renderW,
                    Height = renderH,
                    Process = item.Process
                });

                itemX += itemW;
            }

            curY += stripThickness;
            curH = Math.Max(0, curH - stripThickness);
        }
    }
}
