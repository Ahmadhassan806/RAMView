using System.Threading.Tasks;
using RAMView.Infrastructure.Services;
using Xunit;

namespace RAMView.Tests;

public class RealSystemMonitoringTests
{
    [Fact]
    public async Task RAMMonitor_QueriesRealSystemMemory()
    {
        var monitor = new RAMMonitor();
        var snapshot = await monitor.GetSystemMemorySnapshotAsync();

        Assert.NotNull(snapshot);
        // Ensure non-zero real physical memory (e.g. at least 1 GB on a real modern machine)
        Assert.True(snapshot.TotalPhysicalBytes > 1024UL * 1024UL * 1024UL,
            $"Total physical RAM ({snapshot.FormattedTotal}) must be greater than 1 GB");
        Assert.True(snapshot.UsedPhysicalBytes > 0, "Used physical RAM must be greater than 0");
        Assert.True(snapshot.AvailablePhysicalBytes > 0, "Available physical RAM must be greater than 0");
    }

    [Fact]
    public async Task ProcessMonitor_EnumeratesRealProcesses()
    {
        var iconExtractor = new IconExtractor();
        var processMonitor = new ProcessMonitor(iconExtractor);

        var processes = await processMonitor.GetProcessesAsync(groupProcesses: true, showSystemProcesses: true);

        Assert.NotEmpty(processes);
        // Every real Windows system has at least dozens of processes
        Assert.True(processes.Count >= 10, $"Must find at least 10 processes, found: {processes.Count}");

        // WorkingSet of total tracked memory must be positive
        long totalTracked = 0;
        foreach (var p in processes)
        {
            Assert.False(string.IsNullOrWhiteSpace(p.ProcessName));
            Assert.True(p.WorkingSet64 > 0);
            totalTracked += p.WorkingSet64;
        }

        Assert.True(totalTracked > 100 * 1024 * 1024, "Total tracked Working Set must be > 100 MB");
    }
}
