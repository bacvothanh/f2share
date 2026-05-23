namespace F2Share.Desktop.ViewModels;

public sealed class PeerViewModel : ViewModelBase
{
    private DateTimeOffset _lastSeenUtc;
    private bool _isOnline;

    public required string DeviceId { get; init; }
    public required string DisplayName { get; init; }
    public required string Endpoint { get; init; }

    public DateTimeOffset LastSeenUtc
    {
        get => _lastSeenUtc;
        set
        {
            if (Set(ref _lastSeenUtc, value))
            {
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            if (Set(ref _isOnline, value))
            {
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    public string Status => IsOnline ? $"Online | Last seen {LastSeenUtc:HH:mm:ss}" : "Offline";
}
