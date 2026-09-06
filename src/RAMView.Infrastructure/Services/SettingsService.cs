using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Persists and loads user settings locally via JSON (§19, §20).
/// Strictly offline without any cloud dependencies.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private AppSettings _currentSettings;

    public AppSettings CurrentSettings => _currentSettings;

    public SettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsPath = customPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "RAMView");
            _settingsPath = Path.Combine(folder, "settings.json");
        }

        _currentSettings = new AppSettings();
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                    return _currentSettings;
                }
            }
        }
        catch
        {
            // Fall back to defaults on disk/corrupted file error
        }

        _currentSettings = new AppSettings();
        return _currentSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        try
        {
            string? dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Ignore non-fatal I/O errors during save
        }
    }
}
