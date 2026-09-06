namespace RAMView.Core.Interfaces;

/// <summary>
/// Extracts and caches application icons using dual code paths:
/// 1) Win32 Shell32 extraction for classic Win32 EXEs
/// 2) Package manifest resolution for modern UWP / Store apps.
/// </summary>
public interface IIconExtractor
{
    byte[]? GetIconPng(string? executablePath, string processName);
}
