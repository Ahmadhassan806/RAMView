using System.Threading.Tasks;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Safely terminates processes with critical system process protection.
/// </summary>
public interface IProcessTerminator
{
    bool IsCriticalProcess(int processId, string processName);
    bool CanTerminate(int processId, string processName, out string refusalReason);
    Task<bool> TerminateProcessAsync(int processId);
}
