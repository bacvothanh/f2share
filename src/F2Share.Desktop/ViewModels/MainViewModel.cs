using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using F2Share.Application.Abstractions;
using F2Share.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace F2Share.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private const string ShareId = "default-share";

    private readonly string _shareRootPath;
    private readonly IMetadataStore _metadataStore;
    private readonly IDiscoveryService _discoveryService;
    private readonly IClock _clock;
    private readonly ILogger<MainViewModel> _logger;
    private readonly CancellationTokenSource _lifetimeCts = new();

    private bool _isDarkMode;
    private bool _syncPaused;
    private int _indexedFiles;
    private long _indexedBytes;
    private string _diagnosticsSummary = "Initializing...";

    public MainViewModel(
        IMetadataStore metadataStore,
        IDiscoveryService discoveryService,
        IClock clock,
        ILogger<MainViewModel> logger)
    {
        _metadataStore = metadataStore;
        _discoveryService = discoveryService;
        _clock = clock;
        _logger = logger;

        _shareRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "F2Share");

        SharedFolders = new ObservableCollection<string> { _shareRootPath };
        Peers = [];
        IndexedFiles = [];
        ActiveTransfers =
        [
            new TransferItemViewModel { RelativePath = "bootstrap.index", Progress = 1.0, State = "Ready" }
        ];
        Conflicts = [];
        Logs = [];

        ToggleSyncCommand = new RelayCommand(_ => ToggleSyncPause());
        RefreshNowCommand = new RelayCommand(_ => _ = RefreshIndexedFilesAsync(_lifetimeCts.Token));
        ClearLogsCommand = new RelayCommand(_ => Logs.Clear());

        _ = InitializeAsync(_lifetimeCts.Token);
    }

    public ObservableCollection<string> SharedFolders { get; }
    public ObservableCollection<PeerViewModel> Peers { get; }
    public ObservableCollection<FileSyncEntryViewModel> IndexedFiles { get; }
    public ObservableCollection<TransferItemViewModel> ActiveTransfers { get; }
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; }
    public ObservableCollection<LogEntryViewModel> Logs { get; }

    public string DiagnosticsSummary
    {
        get => _diagnosticsSummary;
        private set => Set(ref _diagnosticsSummary, value);
    }

    public string SyncState => _syncPaused ? "Paused" : "Running";

    public int IndexedFileCount
    {
        get => _indexedFiles;
        private set => Set(ref _indexedFiles, value);
    }

    public string IndexedBytesLabel
    {
        get => ToSize(_indexedBytes);
        private set => _ = value;
    }

    public ICommand ToggleSyncCommand { get; }
    public ICommand RefreshNowCommand { get; }
    public ICommand ClearLogsCommand { get; }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => Set(ref _isDarkMode, value);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _metadataStore.InitializeAsync(cancellationToken).ConfigureAwait(false);
            AddLog("Info", "Desktop runtime initialized.");

            _ = Task.Run(() => RunMetadataLoopAsync(cancellationToken), cancellationToken);
            _ = Task.Run(() => RunDiscoveryLoopAsync(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Desktop initialization failed");
            AddLog("Error", "Failed to initialize desktop runtime.");
        }
    }

    private async Task RunMetadataLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_syncPaused)
            {
                await RefreshIndexedFilesAsync(cancellationToken).ConfigureAwait(false);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunDiscoveryLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var peer in _discoveryService.DiscoverAsync(cancellationToken).ConfigureAwait(false))
            {
                await Dispatcher.UIThread.InvokeAsync(() => UpsertPeer(peer));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Peer discovery loop failed");
            AddLog("Warn", "Peer discovery loop stopped unexpectedly.");
        }
    }

    private async Task RefreshIndexedFilesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<FileFingerprint> items;

        try
        {
            items = await _metadataStore.ListFingerprintsAsync(ShareId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh metadata list");
            AddLog("Error", "Metadata refresh failed.");
            return;
        }

        var projected = items
            .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .Select(item => new FileSyncEntryViewModel
            {
                RelativePath = item.RelativePath,
                SizeLabel = ToSize(item.Length),
                LastWriteLabel = item.LastWriteUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToList();

        var bytes = items.Sum(x => x.Length);
        var diagnostics = $"Sync: {SyncState} | Indexed: {items.Count} files | Size: {ToSize(bytes)} | Peers: {Peers.Count} | Last update: {_clock.UtcNow.LocalDateTime:HH:mm:ss}";

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IndexedFiles.Clear();
            foreach (var entry in projected)
            {
                IndexedFiles.Add(entry);
            }

            IndexedFileCount = items.Count;
            _indexedBytes = bytes;
            OnPropertyChanged(nameof(IndexedBytesLabel));
            DiagnosticsSummary = diagnostics;
        });
    }

    private void UpsertPeer(DiscoveredPeer peer)
    {
        var found = Peers.FirstOrDefault(p => string.Equals(p.DeviceId, peer.DeviceId, StringComparison.Ordinal));
        if (found is null)
        {
            Peers.Add(new PeerViewModel
            {
                DeviceId = peer.DeviceId,
                DisplayName = peer.DisplayName,
                Endpoint = $"{peer.Host}:{peer.Port}",
                IsOnline = true,
                LastSeenUtc = _clock.UtcNow
            });
            AddLog("Info", $"Peer discovered: {peer.DisplayName} ({peer.Host}:{peer.Port})");
            return;
        }

        found.IsOnline = true;
        found.LastSeenUtc = _clock.UtcNow;
    }

    private void ToggleSyncPause()
    {
        _syncPaused = !_syncPaused;
        OnPropertyChanged(nameof(SyncState));
        DiagnosticsSummary = $"Sync: {SyncState} | Indexed: {IndexedFileCount} files | Size: {IndexedBytesLabel} | Peers: {Peers.Count} | Last update: {_clock.UtcNow.LocalDateTime:HH:mm:ss}";
        AddLog("Info", _syncPaused ? "Synchronization paused." : "Synchronization resumed.");
    }

    private void AddLog(string level, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Logs.Insert(0, new LogEntryViewModel
            {
                TimestampUtc = _clock.UtcNow,
                Level = level,
                Message = message
            });

            while (Logs.Count > 300)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        });
    }

    private static string ToSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024):F1} MB";
        return $"{bytes / (1024d * 1024 * 1024):F2} GB";
    }
}
