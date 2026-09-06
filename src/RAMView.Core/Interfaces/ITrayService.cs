using System;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Manages system tray notification icon, tooltips, and context menus (§16).
/// </summary>
public interface ITrayService : IDisposable
{
    void Initialize(
        string initialTooltip,
        Action onShowHide,
        Action onToggleCompact,
        Action onToggleGhost,
        Action onOpenSettings,
        Action onRefresh,
        Action onExit);

    void UpdateTooltip(string tooltip);
    void ShowBalloon(string title, string text);
}
