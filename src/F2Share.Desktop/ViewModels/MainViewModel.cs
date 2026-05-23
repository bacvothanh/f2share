using System.Collections.ObjectModel;
using System.Windows.Input;

namespace F2Share.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private bool _isDarkMode;
    private bool _syncPaused;

    public MainViewModel()
    {
        SharedFolders = new ObservableCollection<string> { "~/Documents/F2Share" };
        Peers = new ObservableCollection<string> { "Device-A (Online)", "Device-B (Offline)" };
        ActiveTransfers = new ObservableCollection<string> { "report.pdf 42%", "videos/demo.mp4 queued" };
        Conflicts = new ObservableCollection<string> { "notes.txt conflict-deviceA-20260523" };
        DiagnosticsSummary = "QUIC: healthy | RTT 21ms | Throughput 18.7 MB/s | Retry queue 0";
        ToggleSyncCommand = new RelayCommand(_ => _syncPaused = !_syncPaused);
    }

    public ObservableCollection<string> SharedFolders { get; }
    public ObservableCollection<string> Peers { get; }
    public ObservableCollection<string> ActiveTransfers { get; }
    public ObservableCollection<string> Conflicts { get; }

    public string DiagnosticsSummary { get; }
    public ICommand ToggleSyncCommand { get; }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => Set(ref _isDarkMode, value);
    }
}
