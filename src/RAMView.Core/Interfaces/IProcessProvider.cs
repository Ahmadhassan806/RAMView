using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RAMView.Core.Models;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Service responsible for enumerating running processes, resolving their Working Set,
/// and aggregating or diffing processes across polling cycles.
/// </summary>
public interface IProcessProvider
{
    ValueTask<IReadOnlyList<ProcessMemoryInfo>> GetProcessesAsync(
        bool groupProcesses,
        bool showSystemProcesses,
        string filterPreset = "All",
        CancellationToken cancellationToken = default);
}
