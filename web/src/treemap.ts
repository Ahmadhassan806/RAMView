export interface ProcessItem {
  id: number;
  name: string;
  workingSetBytes: number;
  instanceCount: number;
  category: 'app' | 'system' | 'background';
  icon: string;
  path?: string;
}

export interface TreemapRect {
  x: number;
  y: number;
  width: number;
  height: number;
  process: ProcessItem;
  percentage: number;
}

export function formatBytes(bytes: number): string {
  if (bytes <= 0) return '0 MB';
  const gb = bytes / (1024 * 1024 * 1024);
  const mb = bytes / (1024 * 1024);
  if (gb >= 1.0) return `${gb.toFixed(2)} GB`;
  return `${mb.toFixed(1)} MB`;
}

interface LayoutItem {
  process: ProcessItem;
  area: number;
  percentage: number;
}

export function computeSquarifiedTreemap(
  processes: ProcessItem[],
  width: number,
  height: number,
  minFloorBytes = 20 * 1024 * 1024,
  maxShareRatio = 0.60,
  padding = 4
): TreemapRect[] {
  if (!processes.length || width <= 10 || height <= 10) return [];

  // 1. Calculate weights applying floor
  const rawWeights = processes.map(p => ({
    process: p,
    rawWeight: Math.max(p.workingSetBytes, minFloorBytes)
  }));

  const totalRaw = rawWeights.reduce((acc, x) => acc + x.rawWeight, 0);
  if (totalRaw <= 0) return [];

  // 2. Anti-dominance rule (max 60% cap per §6)
  const maxAllowed = totalRaw * maxShareRatio;
  let dominantSum = 0;
  let nonDominantSum = 0;
  const dominantSet = new Set<number>();

  rawWeights.forEach((item, idx) => {
    if (item.rawWeight > maxAllowed && rawWeights.length > 1) {
      dominantSet.add(idx);
      dominantSum += maxAllowed;
    } else {
      nonDominantSum += item.rawWeight;
    }
  });

  const adjusted: { process: ProcessItem; weight: number }[] = [];
  if (dominantSet.size > 0 && nonDominantSum > 0) {
    const remainingBudget = Math.max(totalRaw - dominantSum, totalRaw * (1.0 - maxShareRatio));
    rawWeights.forEach((item, idx) => {
      if (dominantSet.has(idx)) {
        adjusted.push({ process: item.process, weight: maxAllowed });
      } else {
        const scaled = (item.rawWeight / nonDominantSum) * remainingBudget;
        adjusted.push({ process: item.process, weight: Math.max(scaled, 1.0) });
      }
    });
  } else {
    rawWeights.forEach(item => {
      adjusted.push({ process: item.process, weight: item.rawWeight });
    });
  }

  // Stable sort descending
  adjusted.sort((a, b) => b.weight - a.weight || a.process.name.localeCompare(b.process.name));

  const totalAdjusted = adjusted.reduce((acc, x) => acc + x.weight, 0);
  const totalArea = width * height;

  const items: LayoutItem[] = adjusted.map(x => ({
    process: x.process,
    area: (x.weight / totalAdjusted) * totalArea,
    percentage: (x.process.workingSetBytes / processes.reduce((s, p) => s + p.workingSetBytes, 0)) * 100
  }));

  const results: TreemapRect[] = [];
  let curX = 0;
  let curY = 0;
  let curW = width;
  let curH = height;

  let currentRow: LayoutItem[] = [];

  for (let i = 0; i < items.length; i++) {
    const item = items[i];
    const shortEdge = Math.min(curW, curH);
    if (shortEdge <= 0.1) break;

    if (currentRow.length === 0) {
      currentRow.push(item);
      continue;
    }

    const currentWorst = worstAspectRatio(currentRow, shortEdge);
    const testRow = [...currentRow, item];
    const testWorst = worstAspectRatio(testRow, shortEdge);

    if (testWorst <= currentWorst) {
      currentRow.push(item);
    } else {
      layoutRow(currentRow, curX, curY, curW, curH, results, padding, (newX, newY, newW, newH) => {
        curX = newX;
        curY = newY;
        curW = newW;
        curH = newH;
      });
      currentRow = [item];
    }
  }

  if (currentRow.length > 0) {
    layoutRow(currentRow, curX, curY, curW, curH, results, padding, (newX, newY, newW, newH) => {
      curX = newX;
      curY = newY;
      curW = newW;
      curH = newH;
    });
  }

  return results;
}

function worstAspectRatio(row: LayoutItem[], sideLength: number): number {
  if (!row.length || sideLength <= 0) return Infinity;
  const sumArea = row.reduce((acc, x) => acc + x.area, 0);
  if (sumArea <= 0) return Infinity;

  const maxArea = Math.max(...row.map(x => x.area));
  const minArea = Math.min(...row.map(x => x.area));

  const sideSq = sideLength * sideLength;
  const sumAreaSq = sumArea * sumArea;

  return Math.max((sideSq * maxArea) / sumAreaSq, sumAreaSq / (sideSq * minArea));
}

function layoutRow(
  row: LayoutItem[],
  curX: number,
  curY: number,
  curW: number,
  curH: number,
  results: TreemapRect[],
  padding: number,
  updateBounds: (x: number, y: number, w: number, h: number) => void
) {
  if (!row.length) return;
  const sumArea = row.reduce((acc, x) => acc + x.area, 0);
  if (sumArea <= 0) return;

  const isHorizontal = curW >= curH;
  const shortEdge = isHorizontal ? curH : curW;
  const stripThickness = sumArea / shortEdge;

  if (isHorizontal) {
    let itemY = curY;
    for (const item of row) {
      const itemH = item.area / stripThickness;
      results.push({
        x: curX + padding / 2,
        y: itemY + padding / 2,
        width: Math.max(0, stripThickness - padding),
        height: Math.max(0, itemH - padding),
        process: item.process,
        percentage: item.percentage
      });
      itemY += itemH;
    }
    updateBounds(curX + stripThickness, curY, Math.max(0, curW - stripThickness), curH);
  } else {
    let itemX = curX;
    for (const item of row) {
      const itemW = item.area / stripThickness;
      results.push({
        x: itemX + padding / 2,
        y: curY + padding / 2,
        width: Math.max(0, itemW - padding),
        height: Math.max(0, stripThickness - padding),
        process: item.process,
        percentage: item.percentage
      });
      itemX += itemW;
    }
    updateBounds(curX, curY + stripThickness, curW, Math.max(0, curH - stripThickness));
  }
}
