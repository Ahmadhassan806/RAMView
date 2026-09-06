using RAMView.Core.Models;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Evaluates system health and visual threshold states (Normal, High, Critical).
/// </summary>
public interface ISystemMetricsProvider
{
    SystemMemoryVisualState EvaluateState(double memoryUsagePercentage, AppSettings settings);
}
