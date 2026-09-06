namespace RAMView.Core.Models;

/// <summary>
/// Persisted configuration options for RAM View application.
/// </summary>
public class AppSettings
{
    public int RefreshIntervalMs { get; set; } = 1000;
    public bool IsGrouped { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool LockPosition { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public double WindowOpacity { get; set; } = 0.96;
    public long MinMemoryFloorBytes { get; set; } = 20 * 1024 * 1024; // 20 MB visual floor (§6)
    public double AntiDominanceMaxAreaShare { get; set; } = 0.60; // 60% max area share (§6)
    public bool ShowSystemProcesses { get; set; } = true;
    public string AppMode { get; set; } = "Overlay"; // "Overlay" or "CompactStrip"
    public double OverlayLeft { get; set; } = 100;
    public double OverlayTop { get; set; } = 100;
    public double OverlayWidth { get; set; } = 540;
    public double OverlayHeight { get; set; } = 380;
    public double HighMemoryThresholdPercent { get; set; } = 80.0;
    public double CriticalMemoryThresholdPercent { get; set; } = 90.0;
    public string Theme { get; set; } = "Dark";
    public string FilterPreset { get; set; } = "All"; // "All", "Applications", "Background", "System", "Highest"
    public string GlobalHotkey { get; set; } = "Ctrl+Shift+R";
}
