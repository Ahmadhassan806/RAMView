using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Manages Windows startup registration (§31) for unpackaged desktop deployment.
/// </summary>
public static class StartupManager
{
    private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "RAMView";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false);
            return key?.GetValue(APP_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true);
            if (key == null) return false;

            if (enable)
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Environment.ProcessPath
                    ?? string.Empty;

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    key.SetValue(APP_NAME, $"\"{exePath}\" --minimized");
                    return true;
                }
                return false;
            }
            else
            {
                if (key.GetValue(APP_NAME) != null)
                {
                    key.DeleteValue(APP_NAME, false);
                }
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
