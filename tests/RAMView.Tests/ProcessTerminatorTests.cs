using RAMView.Infrastructure.Services;
using Xunit;

namespace RAMView.Tests;

public class ProcessTerminatorTests
{
    [Theory]
    [InlineData(0, "Idle")]
    [InlineData(4, "System")]
    [InlineData(1234, "csrss.exe")]
    [InlineData(1234, "csrss")]
    [InlineData(5678, "wininit.exe")]
    [InlineData(9101, "services.exe")]
    [InlineData(1112, "smss.exe")]
    [InlineData(1314, "lsass.exe")]
    [InlineData(1516, "dwm.exe")]
    public void CanTerminate_CriticalProcesses_RefusesWithReason(int pid, string processName)
    {
        var terminator = new ProcessTerminator();

        bool canTerminate = terminator.CanTerminate(pid, processName, out string refusalReason);

        Assert.False(canTerminate, $"Process '{processName}' with PID {pid} must be protected.");
        Assert.NotEmpty(refusalReason);
        Assert.Contains("protected Windows core process", refusalReason);
    }

    [Theory]
    [InlineData(5000, "notepad.exe")]
    [InlineData(6000, "chrome.exe")]
    [InlineData(7000, "CustomApp")]
    public void CanTerminate_UserProcesses_AllowsTermination(int pid, string processName)
    {
        var terminator = new ProcessTerminator();

        bool canTerminate = terminator.CanTerminate(pid, processName, out string refusalReason);

        Assert.True(canTerminate, $"User process '{processName}' should be allowed to terminate.");
        Assert.Empty(refusalReason);
    }
}
