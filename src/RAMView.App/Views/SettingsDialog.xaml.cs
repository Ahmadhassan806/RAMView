using System;
using System.Windows;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;
using RAMView.Infrastructure.Services;

namespace RAMView.App.Views;

public partial class SettingsDialog : Window
{
    private readonly ISettingsService _settingsService;
    private readonly Action<AppSettings> _onSaved;

    public SettingsDialog(ISettingsService settingsService, Action<AppSettings> onSaved)
    {
        InitializeComponent();
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _onSaved = onSaved ?? throw new ArgumentNullException(nameof(onSaved));

        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        var s = _settingsService.CurrentSettings;
        sliderInterval.Value = s.RefreshIntervalMs;
        sliderOpacity.Value = s.WindowOpacity;
        sliderDominance.Value = s.AntiDominanceMaxAreaShare;
        chkAlwaysOnTop.IsChecked = s.AlwaysOnTop;
        chkClickThrough.IsChecked = s.ClickThrough;
        chkLockPosition.IsChecked = s.LockPosition;
        chkGroupProcesses.IsChecked = s.IsGrouped;
        chkShowSystem.IsChecked = s.ShowSystemProcesses;
        chkStartWithWindows.IsChecked = StartupManager.IsStartupEnabled();
    }

    private async void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.CurrentSettings;
        s.RefreshIntervalMs = (int)sliderInterval.Value;
        s.WindowOpacity = sliderOpacity.Value;
        s.AntiDominanceMaxAreaShare = sliderDominance.Value;
        s.AlwaysOnTop = chkAlwaysOnTop.IsChecked ?? true;
        s.ClickThrough = chkClickThrough.IsChecked ?? false;
        s.LockPosition = chkLockPosition.IsChecked ?? false;
        s.IsGrouped = chkGroupProcesses.IsChecked ?? true;
        s.ShowSystemProcesses = chkShowSystem.IsChecked ?? true;
        s.StartWithWindows = chkStartWithWindows.IsChecked ?? false;

        StartupManager.SetStartup(s.StartWithWindows);

        await _settingsService.SaveSettingsAsync(s);
        _onSaved(s);
        Close();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
