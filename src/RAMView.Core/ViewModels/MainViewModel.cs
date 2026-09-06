using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RAMView.Core.Algorithms;
using RAMView.Core.Common;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;
using RAMView.Core.Services;

namespace RAMView.Core.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
    private readonly IMemoryMonitor _memoryMonitor;
    private readonly IProcessProvider _processProvider;
    private readonly ISystemMetricsProvider _metricsProvider;
    private readonly ISettingsService _settingsService;
    private readonly IProcessTerminator _processTerminator;

    private readonly Timer _pollTimer;
    private bool _isPolling;
    private bool _isDisposed;

    private MemorySnapshot? _snapshot;
    private IReadOnlyList<TreemapRectangle> _treemapItems = Array.Empty<TreemapRectangle>();
    private IReadOnlyList<ProcessMemoryInfo> _processes = Array.Empty<ProcessMemoryInfo>();
    private ProcessMemoryInfo? _selectedProcess;
    private string _searchText = string.Empty;
    private string _filterPreset = "All";
    private bool _isGrouped = true;
    private bool _alwaysOnTop = true;
    private bool _clickThrough = false;
    private bool _lockPosition = false;
    private string _appMode = "Overlay";
    private double _windowOpacity = 0.96;
    private SystemMemoryVisualState _visualState = SystemMemoryVisualState.Normal;
    private double _canvasWidth = 520;
    private double _canvasHeight = 280;
    private string _statusMessage = "Ready";
    private readonly SynchronizationContext? _syncContext;

    public MainViewModel(
        IMemoryMonitor memoryMonitor,
        IProcessProvider processProvider,
        ISystemMetricsProvider metricsProvider,
        ISettingsService settingsService,
        IProcessTerminator processTerminator)
    {
        _syncContext = SynchronizationContext.Current;
        _memoryMonitor = memoryMonitor ?? throw new ArgumentNullException(nameof(memoryMonitor));
        _processProvider = processProvider ?? throw new ArgumentNullException(nameof(processProvider));
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _processTerminator = processTerminator ?? throw new ArgumentNullException(nameof(processTerminator));

        // Load persisted settings
        var settings = _settingsService.CurrentSettings;
        _isGrouped = settings.IsGrouped;
        _alwaysOnTop = settings.AlwaysOnTop;
        _clickThrough = settings.ClickThrough;
        _lockPosition = settings.LockPosition;
        _windowOpacity = settings.WindowOpacity;
        _appMode = settings.AppMode;
        _filterPreset = settings.FilterPreset;

        // Initialize commands
        RefreshCommand = new RelayCommand(async () => await PollAsync());
        ToggleModeCommand = new RelayCommand(ToggleMode);
        ToggleGroupCommand = new RelayCommand(ToggleGroup);
        ToggleAlwaysOnTopCommand = new RelayCommand(ToggleAlwaysOnTop);
        ToggleClickThroughCommand = new RelayCommand(ToggleClickThrough);
        ToggleLockPositionCommand = new RelayCommand(ToggleLockPosition);
        SelectProcessCommand = new RelayCommand(param =>
        {
            if (param is ProcessMemoryInfo p) SelectedProcess = p;
            else if (param is TreemapRectangle rect) SelectedProcess = rect.Process;
        });
        ClearSelectionCommand = new RelayCommand(() => SelectedProcess = null);
        OpenFileLocationCommand = new RelayCommand(OpenFileLocation);
        CopyProcessInfoCommand = new RelayCommand(CopyProcessInfo);
        TerminateProcessCommand = new RelayCommand(async () => await TerminateSelectedProcessAsync());

        // Setup polling timer (§17: 500ms - 1000ms configurable)
        int interval = Math.Max(250, settings.RefreshIntervalMs);
        _pollTimer = new Timer(async _ => await PollAsync(), null, 150, interval);
    }

    public MemorySnapshot? Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public IReadOnlyList<TreemapRectangle> TreemapItems
    {
        get => _treemapItems;
        private set => SetProperty(ref _treemapItems, value);
    }

    public IReadOnlyList<ProcessMemoryInfo> Processes
    {
        get => _processes;
        private set => SetProperty(ref _processes, value);
    }

    public ProcessMemoryInfo? SelectedProcess
    {
        get => _selectedProcess;
        set => SetProperty(ref _selectedProcess, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplySearchFilterHighlighting();
            }
        }
    }

    public string FilterPreset
    {
        get => _filterPreset;
        set
        {
            if (SetProperty(ref _filterPreset, value))
            {
                _ = PollAsync();
            }
        }
    }

    public bool IsGrouped
    {
        get => _isGrouped;
        set
        {
            if (SetProperty(ref _isGrouped, value))
            {
                _ = PollAsync();
            }
        }
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set => SetProperty(ref _alwaysOnTop, value);
    }

    public bool ClickThrough
    {
        get => _clickThrough;
        set => SetProperty(ref _clickThrough, value);
    }

    public bool LockPosition
    {
        get => _lockPosition;
        set => SetProperty(ref _lockPosition, value);
    }

    public string AppMode
    {
        get => _appMode;
        set => SetProperty(ref _appMode, value);
    }

    public double WindowOpacity
    {
        get => _windowOpacity;
        set => SetProperty(ref _windowOpacity, value);
    }

    public SystemMemoryVisualState VisualState
    {
        get => _visualState;
        private set => SetProperty(ref _visualState, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public double CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            if (SetProperty(ref _canvasWidth, value))
            {
                RecalculateTreemapLayout();
            }
        }
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            if (SetProperty(ref _canvasHeight, value))
            {
                RecalculateTreemapLayout();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ToggleModeCommand { get; }
    public ICommand ToggleGroupCommand { get; }
    public ICommand ToggleAlwaysOnTopCommand { get; }
    public ICommand ToggleClickThroughCommand { get; }
    public ICommand ToggleLockPositionCommand { get; }
    public ICommand SelectProcessCommand { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand OpenFileLocationCommand { get; }
    public ICommand CopyProcessInfoCommand { get; }
    public ICommand TerminateProcessCommand { get; }

    public event Action<string>? ClipboardRequested;
    public event Action<string, string>? AlertRequested;
    public event Func<string, string, bool>? ConfirmationRequested;

    public async Task PollAsync()
    {
        if (_isPolling || _isDisposed) return;
        _isPolling = true;

        try
        {
            var rawSnapshot = await _memoryMonitor.GetSystemMemorySnapshotAsync();
            var processList = await _processProvider.GetProcessesAsync(
                groupProcesses: _isGrouped,
                showSystemProcesses: _settingsService.CurrentSettings.ShowSystemProcesses,
                filterPreset: _filterPreset);

            long totalTracked = processList.Sum(p => p.WorkingSet64);

            var completeSnapshot = rawSnapshot with
            {
                TotalTrackedBytes = totalTracked,
                Processes = processList
            };

            void ApplyState()
            {
                Snapshot = completeSnapshot;
                Processes = processList;
                VisualState = _metricsProvider.EvaluateState(completeSnapshot.SystemMemoryUsagePercentage, _settingsService.CurrentSettings);
                RecalculateTreemapLayout();
            }

            if (_syncContext != null)
            {
                _syncContext.Post(_ => ApplyState(), null);
            }
            else
            {
                ApplyState();
            }
        }
        catch (Exception ex)
        {
            if (_syncContext != null)
                _syncContext.Post(_ => StatusMessage = $"Notice: {ex.Message}", null);
            else
                StatusMessage = $"Notice: {ex.Message}";
        }
        finally
        {
            _isPolling = false;
        }
    }

    public void RecalculateTreemapLayout()
    {
        if (_processes == null || _processes.Count == 0 || _canvasWidth <= 20 || _canvasHeight <= 20)
        {
            TreemapItems = Array.Empty<TreemapRectangle>();
            return;
        }

        var settings = _settingsService.CurrentSettings;
        var weighted = MemoryCalculator.CalculateLayoutWeights(
            _processes,
            settings.MinMemoryFloorBytes,
            settings.AntiDominanceMaxAreaShare);

        var rects = SquarifiedTreemap.CalculateLayout(
            weighted,
            _canvasWidth,
            _canvasHeight,
            padding: 3.0);

        // Apply search highlighting without altering layout positions (§13)
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string term = _searchText.Trim();
            foreach (var r in rects)
            {
                r.IsHighlighted = r.Process.ProcessName.Contains(term, StringComparison.OrdinalIgnoreCase);
            }
        }

        TreemapItems = rects;
    }

    private void ApplySearchFilterHighlighting()
    {
        if (TreemapItems == null || TreemapItems.Count == 0) return;

        string term = _searchText?.Trim() ?? string.Empty;
        bool hasSearch = !string.IsNullOrEmpty(term);

        foreach (var r in TreemapItems)
        {
            r.IsHighlighted = !hasSearch || r.Process.ProcessName.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // Trigger UI update
        TreemapItems = new List<TreemapRectangle>(TreemapItems);
    }

    private void ToggleMode()
    {
        AppMode = AppMode == "Overlay" ? "CompactStrip" : "Overlay";
    }

    private void ToggleGroup()
    {
        IsGrouped = !IsGrouped;
    }

    private void ToggleAlwaysOnTop()
    {
        AlwaysOnTop = !AlwaysOnTop;
    }

    private void ToggleClickThrough()
    {
        ClickThrough = !ClickThrough;
    }

    private void ToggleLockPosition()
    {
        LockPosition = !LockPosition;
    }

    private void OpenFileLocation()
    {
        if (SelectedProcess?.ExecutablePath != null && File.Exists(SelectedProcess.ExecutablePath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{SelectedProcess.ExecutablePath}\"");
            }
            catch (Exception ex)
            {
                AlertRequested?.Invoke("Failed to open location", ex.Message);
            }
        }
    }

    private void CopyProcessInfo()
    {
        if (SelectedProcess == null) return;

        string info = $"Process: {SelectedProcess.ProcessName}\n" +
                      $"PID: {SelectedProcess.ProcessId}\n" +
                      $"Working Set: {SelectedProcess.FormattedMemory}\n" +
                      $"Tracked Share: {SelectedProcess.FormattedPercentage}\n" +
                      $"Path: {SelectedProcess.ExecutablePath ?? "N/A"}\n" +
                      $"Instances: {SelectedProcess.InstanceCount}";

        ClipboardRequested?.Invoke(info);
        StatusMessage = $"Copied {SelectedProcess.ProcessName} info to clipboard";
    }

    private async Task TerminateSelectedProcessAsync()
    {
        if (SelectedProcess == null) return;

        if (!_processTerminator.CanTerminate(SelectedProcess.ProcessId, SelectedProcess.ProcessName, out string refusalReason))
        {
            AlertRequested?.Invoke("Protected System Process", refusalReason);
            return;
        }

        bool confirmed = ConfirmationRequested?.Invoke(
            "End Process Confirmation",
            $"Are you sure you want to end process '{SelectedProcess.ProcessName}' (PID: {SelectedProcess.ProcessId})?") ?? true;

        if (!confirmed) return;

        bool success = await _processTerminator.TerminateProcessAsync(SelectedProcess.ProcessId);
        if (success)
        {
            StatusMessage = $"Terminated {SelectedProcess.ProcessName}";
            SelectedProcess = null;
            await PollAsync();
        }
        else
        {
            AlertRequested?.Invoke("Termination Failed", $"Could not terminate {SelectedProcess.ProcessName}. You may lack sufficient privileges.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _pollTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
