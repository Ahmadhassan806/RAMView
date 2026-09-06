using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RAMView.Core.Interfaces;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Hardened process termination service enforcing strict denylisting (§12, §23)
/// to prevent killing critical operating system processes.
/// </summary>
public class ProcessTerminator : IProcessTerminator
{
    private static readonly string[] CriticalProcessNames =
    {
        "System",
        "Idle",
        "Registry",
        "smss",
        "smss.exe",
        "csrss",
        "csrss.exe",
        "wininit",
        "wininit.exe",
        "services",
        "services.exe",
        "lsass",
        "lsass.exe",
        "winlogon",
        "winlogon.exe",
        "fontdrvhost",
        "fontdrvhost.exe",
        "dwm",
        "dwm.exe",
        "sihost",
        "sihost.exe",
        "Memory Compression"
    };

    public bool IsCriticalProcess(int processId, string processName)
    {
        if (processId <= 4) return true;

        return CriticalProcessNames.Any(c =>
            string.Equals(c, processName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c, $"{processName}.exe", StringComparison.OrdinalIgnoreCase));
    }

    public bool CanTerminate(int processId, string processName, out string refusalReason)
    {
        if (IsCriticalProcess(processId, processName))
        {
            refusalReason = $"Action blocked: '{processName}' (PID {processId}) is a protected Windows core process. Terminating it would crash or destabilize your system.";
            return false;
        }

        refusalReason = string.Empty;
        return true;
    }

    public Task<bool> TerminateProcessAsync(int processId)
    {
        return Task.Run(() =>
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                if (IsCriticalProcess(processId, process.ProcessName))
                {
                    return false;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
}
