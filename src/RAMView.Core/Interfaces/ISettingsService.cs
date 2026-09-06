using System.Threading.Tasks;
using RAMView.Core.Models;

namespace RAMView.Core.Interfaces;

/// <summary>
/// Persists and retrieves application configuration.
/// </summary>
public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
