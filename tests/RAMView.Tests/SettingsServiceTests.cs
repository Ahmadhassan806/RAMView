using System;
using System.IO;
using System.Threading.Tasks;
using RAMView.Core.Models;
using RAMView.Infrastructure.Services;
using Xunit;

namespace RAMView.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempSettingsPath;

    public SettingsServiceTests()
    {
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"RAMView_Test_{Guid.NewGuid():N}.json");
    }

    [Fact]
    public async Task SettingsService_SavesAndLoads_RoundTrip()
    {
        var service = new SettingsService(_tempSettingsPath);
        var settings = new AppSettings
        {
            RefreshIntervalMs = 750,
            IsGrouped = false,
            AlwaysOnTop = true,
            ClickThrough = true,
            WindowOpacity = 0.85,
            AntiDominanceMaxAreaShare = 0.50
        };

        await service.SaveSettingsAsync(settings);

        var loadedService = new SettingsService(_tempSettingsPath);
        var loaded = await loadedService.LoadSettingsAsync();

        Assert.Equal(750, loaded.RefreshIntervalMs);
        Assert.False(loaded.IsGrouped);
        Assert.True(loaded.AlwaysOnTop);
        Assert.True(loaded.ClickThrough);
        Assert.Equal(0.85, loaded.WindowOpacity, 2);
        Assert.Equal(0.50, loaded.AntiDominanceMaxAreaShare, 2);
    }

    [Fact]
    public async Task SettingsService_NonExistentFile_ReturnsDefaults()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}.json");
        var service = new SettingsService(nonExistentPath);

        var settings = await service.LoadSettingsAsync();

        Assert.NotNull(settings);
        Assert.Equal(1000, settings.RefreshIntervalMs);
        Assert.True(settings.IsGrouped);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempSettingsPath))
            {
                File.Delete(_tempSettingsPath);
            }
        }
        catch { }
    }
}
