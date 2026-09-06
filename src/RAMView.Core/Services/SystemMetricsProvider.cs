using RAMView.Core.Interfaces;
using RAMView.Core.Models;

namespace RAMView.Core.Services;

public class SystemMetricsProvider : ISystemMetricsProvider
{
    public SystemMemoryVisualState EvaluateState(double memoryUsagePercentage, AppSettings settings)
    {
        if (memoryUsagePercentage <= 0) return SystemMemoryVisualState.Unknown;
        if (memoryUsagePercentage >= settings.CriticalMemoryThresholdPercent)
            return SystemMemoryVisualState.Critical;
        if (memoryUsagePercentage >= settings.HighMemoryThresholdPercent)
            return SystemMemoryVisualState.High;
        return SystemMemoryVisualState.Normal;
    }
}
