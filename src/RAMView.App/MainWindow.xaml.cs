using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using RAMView.App.Views;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;
using RAMView.Core.Services;
using RAMView.Core.ViewModels;
using RAMView.Infrastructure.Native;
using RAMView.Infrastructure.Services;

namespace RAMView.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly ITrayService _trayService;
    private readonly LocalApiServer _apiServer;
    private IntPtr _hWnd = IntPtr.Zero;

    public MainWindow()
    {
        InitializeComponent();

        // Instantiate infrastructure services
        var ramMonitor = new RAMMonitor();
        var iconExtractor = new IconExtractor();
        var processMonitor = new ProcessMonitor(iconExtractor);
        var metricsProvider = new SystemMetricsProvider();
        _settingsService = new SettingsService();
        var processTerminator = new ProcessTerminator();
        _hotkeyService = new HotkeyService();
        _trayService = new TrayService();

        // Start Local REST API bridge for web visualizer & companions
        _apiServer = new LocalApiServer(ramMonitor, processMonitor);
        _apiServer.Start();

        // Instantiate ViewModel
        _viewModel = new MainViewModel(
            ramMonitor,
            processMonitor,
            metricsProvider,
            _settingsService,
            processTerminator);

        DataContext = _viewModel;

        // Wire ViewModel UI interactions
        _viewModel.ClipboardRequested += text =>
        {
            try { Clipboard.SetText(text); } catch { }
        };

        _viewModel.AlertRequested += (title, message) =>
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        };

        _viewModel.ConfirmationRequested += (title, message) =>
        {
            var result = MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        };

        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Snapshot) && _viewModel.Snapshot != null)
        {
            // Update tray tooltip throttled per §16
            string tip = $"RAM View: {_viewModel.Snapshot.FormattedUsed} / {_viewModel.Snapshot.FormattedTotal} ({_viewModel.Snapshot.SystemMemoryUsagePercentage:F0}%)";
            _trayService.UpdateTooltip(tip);
        }
        else if (e.PropertyName == nameof(MainViewModel.AlwaysOnTop))
        {
            UpdateAlwaysOnTop();
        }
        else if (e.PropertyName == nameof(MainViewModel.ClickThrough))
        {
            UpdateWindowExStyles();
        }
        else if (e.PropertyName == nameof(MainViewModel.AppMode))
        {
            if (_viewModel.AppMode == "CompactStrip")
            {
                Height = 80;
                MinHeight = 60;
            }
            else
            {
                Height = 460;
                MinHeight = 180;
            }
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hWnd = new WindowInteropHelper(this).Handle;

        // Apply Win32 dark mode and DWM glass frame (§15)
        int darkMode = 1;
        NativeMethods.DwmSetWindowAttribute(_hWnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        Topmost = _viewModel.AlwaysOnTop;

        if (_viewModel.ClickThrough)
        {
            UpdateWindowExStyles();
        }

        // Register Global Hotkey (Ctrl+Shift+R)
        _hotkeyService.Register(_hWnd, _settingsService.CurrentSettings.GlobalHotkey);
        _hotkeyService.HotkeyPressed += (s, ev) => ToggleWindowVisibility();

        // Initialize System Tray Icon (§16)
        _trayService.Initialize(
            "RAM View — Live System Memory",
            onShowHide: ToggleWindowVisibility,
            onToggleCompact: () => _viewModel.ToggleModeCommand.Execute(null),
            onToggleGhost: () =>
            {
                _viewModel.ClickThrough = !_viewModel.ClickThrough;
                UpdateWindowExStyles();
            },
            onOpenSettings: () => OnOpenSettingsClicked(this, new RoutedEventArgs()),
            onRefresh: () => _viewModel.RefreshCommand.Execute(null),
            onExit: ExitApplication);
    }

    private void ToggleWindowVisibility()
    {
        if (Visibility == Visibility.Visible)
        {
            Hide();
        }
        else
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    private void UpdateWindowExStyles()
    {
        if (_hWnd == IntPtr.Zero) return;

        int exStyle = NativeMethods.GetWindowLong(_hWnd, NativeMethods.GWL_EXSTYLE);

        if (_viewModel.ClickThrough)
        {
            exStyle |= NativeMethods.WS_EX_TRANSPARENT;
        }
        else
        {
            exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLong(_hWnd, NativeMethods.GWL_EXSTYLE, exStyle);
    }

    private void UpdateAlwaysOnTop()
    {
        Topmost = _viewModel.AlwaysOnTop;
    }

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_viewModel.LockPosition && !_viewModel.ClickThrough)
        {
            DragMove();
        }
    }

    private void OnAlwaysOnTopToggled(object sender, RoutedEventArgs e)
    {
        UpdateAlwaysOnTop();
    }

    private void OnClickThroughToggled(object sender, RoutedEventArgs e)
    {
        UpdateWindowExStyles();
    }

    private void OnFilterPresetChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string preset)
        {
            _viewModel.FilterPreset = preset;
        }
    }

    private void OnOpenSettingsClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog(_settingsService, updatedSettings =>
        {
            _viewModel.WindowOpacity = updatedSettings.WindowOpacity;
            _viewModel.AlwaysOnTop = updatedSettings.AlwaysOnTop;
            _viewModel.ClickThrough = updatedSettings.ClickThrough;
            _viewModel.LockPosition = updatedSettings.LockPosition;
            _viewModel.IsGrouped = updatedSettings.IsGrouped;
            _ = _viewModel.PollAsync();
        });
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void OnMinimizeClicked(object sender, RoutedEventArgs e)
    {
        Hide();
        _trayService.ShowBalloon("RAM View", "RAM View is running in the background. Access anytime from the system tray.");
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void ExitApplication()
    {
        _apiServer.Dispose();
        _trayService.Dispose();
        _hotkeyService.Dispose();
        _viewModel.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Minimize to tray on close button (§15)
        e.Cancel = true;
        Hide();
    }
}