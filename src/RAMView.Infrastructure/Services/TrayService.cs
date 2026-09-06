using System;
using System.Drawing;
using System.Windows.Forms;
using RAMView.Core.Interfaces;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// System tray service implementation using Win32 NotifyIcon (§16).
/// Keeps tray lifecycle and rate-limited tooltip updates properly encapsulated.
/// </summary>
public class TrayService : ITrayService
{
    private NotifyIcon? _notifyIcon;
    private string _lastTooltip = string.Empty;

    public void Initialize(
        string initialTooltip,
        Action onShowHide,
        Action onToggleCompact,
        Action onToggleGhost,
        Action onOpenSettings,
        Action onRefresh,
        Action onExit)
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = initialTooltip.Length > 63 ? initialTooltip.Substring(0, 63) : initialTooltip
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show / Hide RAM View", null, (s, e) => onShowHide());
        menu.Items.Add("Compact Mode", null, (s, e) => onToggleCompact());
        menu.Items.Add("Toggle Ghost (Click-Through)", null, (s, e) => onToggleGhost());
        menu.Items.Add("Settings", null, (s, e) => onOpenSettings());
        menu.Items.Add("Refresh", null, (s, e) => onRefresh());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (s, e) => onExit());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (s, e) => onShowHide();
    }

    public void UpdateTooltip(string tooltip)
    {
        if (_notifyIcon == null || tooltip == _lastTooltip) return;

        string sanitized = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
        _lastTooltip = tooltip;

        try
        {
            _notifyIcon.Text = sanitized;
        }
        catch
        {
            // Ignore transient Windows shell tooltip update errors
        }
    }

    public void ShowBalloon(string title, string text)
    {
        _notifyIcon?.ShowBalloonTip(1500, title, text, ToolTipIcon.Info);
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        GC.SuppressFinalize(this);
    }
}
