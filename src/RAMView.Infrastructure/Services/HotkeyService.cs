using System;
using System.Windows.Interop;
using RAMView.Core.Interfaces;
using RAMView.Infrastructure.Native;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Manages global hotkey registration using Win32 RegisterHotKey (§15).
/// </summary>
public class HotkeyService : IHotkeyService
{
    private const int HOTKEY_ID = 9001;
    private IntPtr _hWnd = IntPtr.Zero;
    private HwndSource? _source;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;

    public bool Register(IntPtr hWnd, string hotkeyChord)
    {
        Unregister();

        if (hWnd == IntPtr.Zero) return false;
        _hWnd = hWnd;

        ParseChord(hotkeyChord, out uint modifiers, out uint vk);

        _isRegistered = NativeMethods.RegisterHotKey(_hWnd, HOTKEY_ID, modifiers | NativeMethods.MOD_NOREPEAT, vk);

        if (_isRegistered)
        {
            _source = HwndSource.FromHwnd(_hWnd);
            _source?.AddHook(HwndHook);
        }

        return _isRegistered;
    }

    public void Unregister()
    {
        if (_isRegistered && _hWnd != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_hWnd, HOTKEY_ID);
            _source?.RemoveHook(HwndHook);
            _source = null;
            _isRegistered = false;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ParseChord(string chord, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0x52; // Default 'R'

        if (string.IsNullOrWhiteSpace(chord))
        {
            modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
            return;
        }

        var parts = chord.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                modifiers |= NativeMethods.MOD_CONTROL;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                modifiers |= NativeMethods.MOD_SHIFT;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                modifiers |= NativeMethods.MOD_ALT;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                modifiers |= NativeMethods.MOD_WIN;
            else if (part.Length == 1)
            {
                char c = char.ToUpperInvariant(part[0]);
                if (c >= 'A' && c <= 'Z')
                    vk = (uint)c;
                else if (c >= '0' && c <= '9')
                    vk = (uint)c;
            }
        }
    }

    public void Dispose()
    {
        Unregister();
        GC.SuppressFinalize(this);
    }
}
