using System.Threading;
using System.Threading.Tasks;
using RAMView.Core.Models;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Service responsible for acquiring system-wide RAM metrics (total physical, used, available).
/// </summary>
public interface IMemoryMonitor
{
    ValueTask<MemorySnapshot> GetSystemMemorySnapshotAsync(CancellationToken cancellationToken = default);
}
