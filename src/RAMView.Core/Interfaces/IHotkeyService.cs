using System;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Service for managing global hotkey registration and notification.
/// </summary>
public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Register(IntPtr hWnd, string hotkeyChord);
    void Unregister();
}
